#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

using MethodAttributes = dnlib.DotNet.MethodAttributes;
using TypeAttributes = dnlib.DotNet.TypeAttributes;

namespace TerrariaPatcher;

public abstract class PatchSet {
	public abstract string Name { get; }
	public abstract Version Version { get; }
	public abstract string Description { get; }
	public virtual string TargetModuleName => "Terraria";
	public virtual IReadOnlyCollection<Type>? Dependencies => null;

	public virtual void BeforeApply() { }
	public virtual void AfterApply() { }

	public IReadOnlyList<Patch> Patches { get; }

	protected PatchSet() {
		var patches = new List<Patch>();
		foreach (var patchType in this.GetType().GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)) {
			if (!patchType.IsAbstract && typeof(Patch).IsAssignableFrom(patchType.BaseType)) {
				patches.Add((Patch) Activator.CreateInstance(patchType));
			}
		}
		this.Patches = patches.AsReadOnly();
	}

	protected static TypeDef ImportType(Type type, ModuleDef targetModule)
		=> Program.TargetModules.First(t => t.ModuleDef == targetModule).ImportType(type);
	protected static TypeDef ImportType(Type type, string targetModule)
		=> Program.TargetModules.First(t => t.ModuleDef.Assembly.Name == targetModule).ImportType(type);

	protected static void CopyFileToOutputDirectory(string fileName) {
		var outPath = Path.Combine(Path.GetDirectoryName(Program.TargetModules[0].OutputPath), fileName);
		if (!File.Exists(outPath))
			File.Copy(fileName, outPath);
	}

	public void Apply(PatchProgressHandler? progressCallback) {
		this.BeforeApply();

		var targetModule = Program.GetTargetModule(this.TargetModuleName);
		var patchSetTypeDef = targetModule.CurrentModuleDef.Import(this.GetType()).ResolveTypeDefThrow();

		// Create a new type to copy patch set static members into, so that patches can call them.
		var copyPatchSetType = new TypeDefUser(this.GetType().Namespace, this.GetType().Name, targetModule.ModuleDef.CorLibTypes.Object.TypeDefOrRef) {
			Attributes = TypeAttributes.AutoLayout | TypeAttributes.Class | TypeAttributes.AnsiClass | TypeAttributes.Abstract | TypeAttributes.Sealed
		};
		targetModule.ModuleDef.Types.Add(copyPatchSetType);

		int i = 0;
		foreach (var patch in this.Patches) {
			progressCallback?.Invoke(i, patch.GetType().Name);
			i++;

			patch.currentPatchTargetModule = targetModule;

			var patchTypeDef = patch.currentPatchTargetModule.CurrentModuleDef.Import(patch.GetType()).ResolveTypeDefThrow();

			// Create a new type to copy patch static members into, so that patches can call them.
			var copyPatchType = new TypeDefUser(patch.GetType().Name, targetModule.ModuleDef.CorLibTypes.Object.TypeDefOrRef) {
				Attributes = TypeAttributes.AutoLayout | TypeAttributes.Class | TypeAttributes.AnsiClass | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.NestedAssembly
			};
			copyPatchSetType.NestedTypes.Add(copyPatchType);
			patch.currentPatchSetType = copyPatchSetType;
			patch.currentPatchType = copyPatchType;

			var methodDefs = patch.TargetMethod.GetMethodDefs();
			foreach (var methodDef in methodDefs) {
				if (methodDef.Module != targetModule.ModuleDef)
					throw new NotSupportedException("Patch target method does not match patch set target module");
				patch.PatchMethodBody(methodDef);

				// Ensure that any short branch instructions that now have to reach too far are changed to long branch instructions.
				methodDef.Body.SimplifyBranches();
				methodDef.Body.OptimizeBranches();
				methodDef.Body.OptimizeMacros();
			}

			// Copy static members and nested types from the patch type.
			foreach (var type in patch.GetType().GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)) {
				if (!typeof(IEnumerable<MethodDef>).IsAssignableFrom(type)) {
					// Skip MethodDef iterator types.
					patchTypeDef.Module.Import(type).ResolveTypeDefThrow().DeclaringType = copyPatchType;
				}
			}
			foreach (var property in patchTypeDef.Properties.Where(f => (f.GetMethod ?? f.SetMethod).IsStatic).ToList()) {
				property.DeclaringType = copyPatchType;
			}
			foreach (var field in patchTypeDef.Fields.Where(f => f.IsStatic).ToList()) {
				field.DeclaringType = copyPatchType;
			}
			foreach (var method in patchTypeDef.Methods.Where(f => f.IsStatic).ToList()) {
				method.DeclaringType = copyPatchType;
			}

			patch.currentPatchTargetModule = null;
			patch.currentPatchSetType = null;
			patch.currentPatchType = null;
		}

		// Copy static members and nested types from the patch set type.
		foreach (var type in this.GetType().GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
			.Where(t => !typeof(Patch).IsAssignableFrom(t))) {
			patchSetTypeDef.Module.Import(type).ResolveTypeDefThrow().DeclaringType = copyPatchSetType;
		}
		foreach (var property in patchSetTypeDef.Properties.Where(f => (f.GetMethod ?? f.SetMethod).IsStatic).ToList()) {
			property.DeclaringType = copyPatchSetType;
		}
		foreach (var field in patchSetTypeDef.Fields.ToList()) {
			var attributeDef = field.CustomAttributes.FirstOrDefault(a => a.TypeFullName == typeof(ReversePatchAttribute).FullName);
			if (attributeDef is not null) {
				if (!field.IsStatic) throw new InvalidOperationException("Reverse patch field must be static.");
				field.DeclaringType = copyPatchSetType;
				field.CustomAttributes.Remove(attributeDef);

				var targetMethod = this.GetType().GetField(field.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
					.GetCustomAttribute<ReversePatchAttribute>().PatchTarget.GetMethodDefs().Single();

				// Make it at least internal access.
				if ((targetMethod.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Private)
					targetMethod.Attributes = (targetMethod.Attributes & ~MethodAttributes.MemberAccessMask) | MethodAttributes.Assembly;
				else if ((targetMethod.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamANDAssem)
					targetMethod.Attributes = (targetMethod.Attributes & ~MethodAttributes.MemberAccessMask) | MethodAttributes.Assembly;
				else if ((targetMethod.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Family)
					targetMethod.Attributes = (targetMethod.Attributes & ~MethodAttributes.MemberAccessMask) | MethodAttributes.FamORAssem;

				var delegateConstructorRef = new MemberRefUser(targetModule.ModuleDef, ".ctor",
					MethodSig.CreateInstance(targetModule.ModuleDef.CorLibTypes.Void, targetModule.ModuleDef.CorLibTypes.Object, targetModule.ModuleDef.CorLibTypes.IntPtr),
					new TypeSpecUser(field.FieldType));

				// Add code to the static constructor to set the field.
				var staticConstructor = copyPatchSetType.FindOrCreateStaticConstructor();
				var instructions = staticConstructor.Body.Instructions;
				instructions.Insert(instructions.Count - 1, OpCodes.Ldnull.ToInstruction());
				instructions.Insert(instructions.Count - 1, OpCodes.Ldftn.ToInstruction(targetMethod));
				instructions.Insert(instructions.Count - 1, OpCodes.Newobj.ToInstruction(delegateConstructorRef));
				instructions.Insert(instructions.Count - 1, OpCodes.Stsfld.ToInstruction(field));
			} else if (field.IsStatic)
				field.DeclaringType = copyPatchSetType;
		}
		foreach (var method in patchSetTypeDef.Methods.Where(f => f.IsStatic).ToList()) {
			method.DeclaringType = copyPatchSetType;
		}

		// Copy compiler-generated types.
		if (typeof(Program).Assembly.GetType("System.Runtime.CompilerServices.NullableAttribute") is Type t1)
			ImportType(t1, targetModule.ModuleDef);
		if (typeof(Program).Assembly.GetType("System.Runtime.CompilerServices.NullableContextAttribute") is Type t2)
			ImportType(t2, targetModule.ModuleDef);
		if (typeof(Program).Assembly.GetType("Microsoft.CodeAnalysis.EmbeddedAttribute") is Type t3)
			ImportType(t3, targetModule.ModuleDef);
		if (typeof(Program).Assembly.GetType("<PrivateImplementationDetails>") is Type t4)
			ImportType(t4, targetModule.ModuleDef);

		this.AfterApply();
	}
}

public delegate void PatchProgressHandler(int patchesApplied, string patchName);