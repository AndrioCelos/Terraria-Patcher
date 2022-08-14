#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

using TerrariaPatcher.Mods;

using MethodAttributes = dnlib.DotNet.MethodAttributes;
using TypeAttributes = dnlib.DotNet.TypeAttributes;

namespace TerrariaPatcher;

public abstract class PatchSet {
	public abstract string Name { get; }
	public abstract Version Version { get; }
	public abstract string Description { get; }
	public virtual string TargetModuleName => "Terraria";
	public virtual IReadOnlyCollection<Type>? Dependencies => null;
	protected internal IPatchSetConfig? Config { get; internal set; }

	public virtual void BeforeApply() { }
	public virtual void AfterApply() { }

	public IReadOnlyList<Patch> Patches { get; }

	protected PatchSet() {
		var patches = new List<Patch>();
		foreach (var patchType in this.GetType().GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)) {
			if (!patchType.IsAbstract && typeof(Patch).IsAssignableFrom(patchType.BaseType)) {
				var patch = (Patch) Activator.CreateInstance(patchType);
				patch.PatchSet = this;
				patches.Add(patch);
			}
		}
		this.Patches = patches.AsReadOnly();
	}

	protected static TypeDef ImportType(Type type, ModuleDef targetModule)
		=> Program.TargetModules.First(t => t.ModuleDef == targetModule).ImportType(type);
	protected static TypeDef ImportType(Type type, string targetModule)
		=> Program.TargetModules.First(t => t.ModuleDef.Assembly.Name == targetModule).ImportType(type);

	protected static void CopyFileToOutputDirectory(string path)
		=> CopyFileToOutputDirectory(path, false);
	protected static void CopyFileToOutputDirectory(string path, bool overwrite) {
		var outPath = Path.Combine(Path.GetDirectoryName(Program.TargetModules[0].OutputPath), path);
		if (overwrite || !File.Exists(outPath))
			File.Copy(path, outPath, overwrite);
	}
	protected static void CopyFileToOutputDirectory(Stream stream, string outputFileName)
		=> CopyFileToOutputDirectory(stream, outputFileName, false);
	protected static void CopyFileToOutputDirectory(Stream stream, string outputFileName, bool overwrite) {
		var outPath = Path.Combine(Path.GetDirectoryName(Program.TargetModules[0].OutputPath), outputFileName);
		if (overwrite || !File.Exists(outPath)) {
			using var outStream = File.Open(outPath, FileMode.Create, FileAccess.Write);
			stream.CopyTo(outStream);
		}
	}
	protected static void CopyFileToOutputDirectory(byte[] bytes, string outputFileName)
		=> CopyFileToOutputDirectory(bytes, outputFileName, false);
	protected static void CopyFileToOutputDirectory(ArraySegment<byte> bytes, string outputFileName)
		=> CopyFileToOutputDirectory(bytes, outputFileName, false);
	protected static void CopyFileToOutputDirectory(byte[] bytes, string outputFileName, bool overwrite) {
		var outPath = Path.Combine(Path.GetDirectoryName(Program.TargetModules[0].OutputPath), outputFileName);
		if (overwrite || !File.Exists(outPath))
			File.WriteAllBytes(outPath, bytes);
	}
	protected static void CopyFileToOutputDirectory(ArraySegment<byte> bytes, string outputFileName, bool overwrite) {
		var outPath = Path.Combine(Path.GetDirectoryName(Program.TargetModules[0].OutputPath), outputFileName);
		if (overwrite || !File.Exists(outPath)) {
			using var outStream = File.Open(outPath, FileMode.Create, FileAccess.Write);
			outStream.Write(bytes.Array, bytes.Offset, bytes.Count);
		}
	}

	public void Apply(PatchProgressHandler? progressCallback) {
		var targetModule = Program.GetTargetModule(this.TargetModuleName);

		var patchVersionAttributeType = ImportType(typeof(PatchVersionAttribute), this.TargetModuleName);

		var copyPatchSetType = targetModule.ModuleDef.Types.FirstOrDefault(t => t.Namespace == this.GetType().Namespace && t.Name == this.GetType().Name);
		if (copyPatchSetType is not null) {
			// Patch seems to have already been applied. Trying to apply it again would probably fail.
			var patchVersionAttribute = copyPatchSetType.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName == patchVersionAttributeType.FullName);
			Version existingVersion;
			if (patchVersionAttribute is not null) {
				var args = patchVersionAttribute.ConstructorArguments;
				existingVersion = args.Count switch {
					2 => new Version(args[0].Value as int? ?? 0, args[1].Value as int? ?? 0),
					3 => new Version(args[0].Value as int? ?? 0, args[1].Value as int? ?? 0, args[2].Value as int? ?? 0),
					4 => new Version(args[0].Value as int? ?? 0, args[1].Value as int? ?? 0, args[2].Value as int? ?? 0, args[3].Value as int? ?? 0),
					_ => new()
				};
			} else {
				existingVersion = new();
			}
			if (existingVersion < this.Version) throw new InvalidOperationException($"Cannot apply the patch when an older version ({this.Name} v{existingVersion}) is already installed.");
			return;
		}

		targetModule.Modified = true;
		this.BeforeApply();
		var patchSetTypeDef = targetModule.CurrentModuleDef.Import(this.GetType()).ResolveTypeDefThrow();

		// Create a new type to copy patch set static members into, so that patches can call them.
		copyPatchSetType = new TypeDefUser(this.GetType().Namespace, this.GetType().Name, targetModule.ModuleDef.CorLibTypes.Object.TypeDefOrRef) {
			Attributes = TypeAttributes.AutoLayout | TypeAttributes.Class | TypeAttributes.AnsiClass | TypeAttributes.Abstract | TypeAttributes.Sealed
		};
		if (this.Version is not null && this.Version.Minor >= 0) {
			var attributeArgs = new CAArgument[this.Version.Revision >= 0 ? 4 : this.Version.Build >= 0 ? 3 : 2];
			if (attributeArgs.Length > 0) attributeArgs[0] = new(targetModule.ModuleDef.CorLibTypes.Int32, this.Version.Major);
			if (attributeArgs.Length > 1) attributeArgs[1] = new(targetModule.ModuleDef.CorLibTypes.Int32, this.Version.Minor);
			if (attributeArgs.Length > 2) attributeArgs[2] = new(targetModule.ModuleDef.CorLibTypes.Int32, this.Version.Build);
			if (attributeArgs.Length > 3) attributeArgs[3] = new(targetModule.ModuleDef.CorLibTypes.Int32, this.Version.Revision);
			copyPatchSetType.CustomAttributes.Add(new(patchVersionAttributeType.FindInstanceConstructors().FirstOrDefault(c => c.Parameters.Count == attributeArgs.Length + 1), attributeArgs));
		}
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

			// Patch the target methods.
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
			this.CopyStaticMembers(patch.GetType(), patchTypeDef, copyPatchType);

			patch.currentPatchTargetModule = null;
			patch.currentPatchSetType = null;
			patch.currentPatchType = null;
		}

		// Copy static members and nested types from the patch set type.
		this.CopyStaticMembers(this.GetType(), patchSetTypeDef, copyPatchSetType);

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

	private void CopyStaticMembers(Type originalType, TypeDef originalTypeDef, TypeDef copyTypeDef) {
		foreach (var type in originalType.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)) {
			if (type.GetCustomAttribute<NoCopyToTargetAttribute>() is null
				&& !typeof(IPatchSetConfig).IsAssignableFrom(type) && !typeof(IEnumerable<MethodDef>).IsAssignableFrom(type)
				&& !typeof(Patch).IsAssignableFrom(type) && !typeof(IEnumerable<MethodDef>).IsAssignableFrom(type))
				// Skip MethodDef iterator types.
				originalTypeDef.Module.Import(type).ResolveTypeDefThrow().DeclaringType = copyTypeDef;
		}
		foreach (var property in originalTypeDef.Properties.Where(p => (p.GetMethod ?? p.SetMethod).IsStatic
			&& GetCustomAttribute(p.CustomAttributes, typeof(NoCopyToTargetAttribute)) is null).ToList()) {
			property.DeclaringType = copyTypeDef;
		}
		foreach (var ev in originalTypeDef.Events.Where(ev => (ev.AddMethod ?? ev.RemoveMethod ?? ev.InvokeMethod).IsStatic
			&& GetCustomAttribute(ev.CustomAttributes, typeof(NoCopyToTargetAttribute)) is null).ToList()) {
			ev.DeclaringType = copyTypeDef;
		}
		foreach (var field in originalTypeDef.Fields.ToList()) {
			var attributeDef = GetCustomAttribute(field.CustomAttributes, typeof(ReversePatchAttribute));
			if (attributeDef is not null) {
				if (!field.IsStatic) throw new InvalidOperationException("Reverse patch field must be static.");
				field.DeclaringType = copyTypeDef;
				field.CustomAttributes.Remove(attributeDef);

				var targetMethod = this.GetType().GetField(field.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
					.GetCustomAttribute<ReversePatchAttribute>().PatchTarget.GetMethodDefs().Single();

				// Change the target method to at least internal access if necessary.
				if ((targetMethod.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Private)
					targetMethod.Attributes = (targetMethod.Attributes & ~MethodAttributes.MemberAccessMask) | MethodAttributes.Assembly;
				else if ((targetMethod.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamANDAssem)
					targetMethod.Attributes = (targetMethod.Attributes & ~MethodAttributes.MemberAccessMask) | MethodAttributes.Assembly;
				else if ((targetMethod.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Family)
					targetMethod.Attributes = (targetMethod.Attributes & ~MethodAttributes.MemberAccessMask) | MethodAttributes.FamORAssem;

				var delegateConstructorRef = new MemberRefUser(copyTypeDef.Module, ".ctor",
					MethodSig.CreateInstance(copyTypeDef.Module.CorLibTypes.Void, copyTypeDef.Module.CorLibTypes.Object, copyTypeDef.Module.CorLibTypes.IntPtr),
					new TypeSpecUser(field.FieldType));

				// Add code to the static constructor to set the field.
				var staticConstructor = copyTypeDef.FindOrCreateStaticConstructor();
				var instructions = staticConstructor.Body.Instructions;
				instructions.Insert(instructions.Count - 1, OpCodes.Ldnull.ToInstruction());
				instructions.Insert(instructions.Count - 1, OpCodes.Ldftn.ToInstruction(targetMethod));
				instructions.Insert(instructions.Count - 1, OpCodes.Newobj.ToInstruction(delegateConstructorRef));
				instructions.Insert(instructions.Count - 1, OpCodes.Stsfld.ToInstruction(field));
			} else if (field.IsStatic && GetCustomAttribute(field.CustomAttributes, typeof(NoCopyToTargetAttribute)) is null)
				field.DeclaringType = copyTypeDef;
		}
		foreach (var method in originalTypeDef.Methods
			.Where(m => m.IsStatic && GetCustomAttribute(m.CustomAttributes, typeof(NoCopyToTargetAttribute)) is null).ToList()) {
			method.DeclaringType = copyTypeDef;
		}
	}

	private static CustomAttribute? GetCustomAttribute(CustomAttributeCollection attributes, Type type)
		=> attributes.FirstOrDefault(a => a.TypeFullName == type.FullName);

	internal ICollection<KeyValuePair<string, Command>> GetCommands() {
		foreach (var patch in this.Patches.OfType<MainInitializePatch>()) {
			var prefixMethod = patch.GetType().GetMethod("Prefix", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
			if (prefixMethod is not null && prefixMethod.GetParameters().Length == 0) {
				CommandManager.Commands.Clear();
				prefixMethod.Invoke(null, null);
				return CommandManager.Commands.ToList();
			}
		}
		return Array.Empty<KeyValuePair<string, Command>>();
	}
}

public delegate void PatchProgressHandler(int patchesApplied, string patchName);
