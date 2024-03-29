#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

using dnlib.DotNet;

using TerrariaPatcherCommon;

namespace TerrariaPatcher;

internal static class Program {
	internal const bool SKIP_ORIGINAL = false;
	internal const bool DONT_SKIP_ORIGINAL = true;

	internal static AssemblyResolver AssemblyResolver { get; } = new();
	internal static TargetModule[] TargetModules { get; private set; } = Array.Empty<TargetModule>();

	/// <summary>
	///  The main entry point for the application.
	/// </summary>
	[STAThread]
	static int Main(string[] args) {
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(false);

		var exePath = CommonUtils.GuiGetTerrariaExePath(args);
		var reLogicDllPath = Path.Combine(Path.GetDirectoryName(exePath), "ReLogic.dll");
		if (exePath is null) return 1;

		CommonUtils.ExtractResource(exePath, "Terraria.Libraries.ReLogic.ReLogic.dll", reLogicDllPath);

		TargetModules = new TargetModule[] {
			new(exePath, Path.Combine(Path.GetDirectoryName(exePath), "Terraria.patched.exe")),
			new(reLogicDllPath, Path.Combine(Path.GetDirectoryName(exePath), "ReLogic.patched.dll"))
		};

		AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
		
		var patchSets = Assembly.GetExecutingAssembly().DefinedTypes.Where(t => t.BaseType == typeof(PatchSet))
			.Select(t => (PatchSet) Activator.CreateInstance(t));

		var mainForm = new MainForm(patchSets);
		Application.Run(mainForm);
		return 0;
	}

	private static Assembly? CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args) {
		if (TargetModules is not null) {
			var assemblyName = new AssemblyName(args.Name).Name;
			// Include the patch target assemblies.
			foreach (var targetModule in TargetModules) {
				if (assemblyName == Path.GetFileNameWithoutExtension(targetModule.InputPath))
					return Assembly.LoadFrom(targetModule.InputPath);
			}
			// Include other assembles that are embedded in Terraria.
			var terrariaResource = TargetModules[0].ModuleDef.Resources.OfType<EmbeddedResource>()
				.FirstOrDefault(r => r.Name.EndsWith($"{assemblyName}.dll"));
			if (terrariaResource != null)
				return Assembly.Load(terrariaResource.CreateReader().ToArray());
		}
		return null;
	}

	internal static TargetModule GetTargetModule(ModuleDef moduleDef)
		=> TargetModules.FirstOrDefault(m => m.ModuleDef == moduleDef)
			?? throw new KeyNotFoundException($"Module is not a target module: {moduleDef}");
	internal static TargetModule GetTargetModule(Module module)
		=> TargetModules.FirstOrDefault(m => m.ModuleDef.Name == module.Name)
			?? throw new KeyNotFoundException($"Module is not a target module: {module}");
	internal static TargetModule GetTargetModule(string name)
		=> TargetModules.FirstOrDefault(m => m.ModuleDef.Assembly.Name == name)
			?? throw new KeyNotFoundException($"Module is not a target module: {name}");

	internal enum AssignabilityResult {
		NotAssignable = 0,
		Assignable = 1,
		AssignableWithBox = 2
	}

	/// <summary>Determines whether the source <see cref="TypeSig"/> is assignable to the target <see cref="TypeSig"/> and whether boxing is required.</summary>
	internal static AssignabilityResult TypeIsAssignableTo(TypeSig source, TypeSig target) {
		if (source.ElementType == ElementType.Void || target.ElementType == ElementType.Void)
			return AssignabilityResult.NotAssignable;
		if (source == target) return AssignabilityResult.Assignable;

		switch (source.ElementType) {
			case ElementType.Boolean:
			case ElementType.Char:
			case ElementType.I1:
			case ElementType.U1:
			case ElementType.I2:
			case ElementType.U2:
			case ElementType.I4:
			case ElementType.U4:
			case ElementType.I8:
			case ElementType.U8:
			case ElementType.R4:
			case ElementType.R8:
			case ElementType.TypedByRef:
			case ElementType.I:
			case ElementType.U:
				// Core value types are assignable to the same value type and under the conditions for all value types.
				return target.ElementType == source.ElementType
					? AssignabilityResult.Assignable
					: target.ElementType == ElementType.Object
						|| (target.ElementType is ElementType.Class or ElementType.ValueType or ElementType.GenericInst
							&& TypeIsAssignableTo(((CorLibTypeSig) source).TypeDefOrRef, target))
					? AssignabilityResult.AssignableWithBox
					: AssignabilityResult.NotAssignable;
			case ElementType.String:
				// string is assignable to string and object, and under the conditions for all reference types.
				return target.ElementType is ElementType.String or ElementType.Object
					|| (target.ElementType is ElementType.Class or ElementType.ValueType or ElementType.GenericInst
						&& TypeIsAssignableTo(((CorLibTypeSig) source).TypeDefOrRef, target))
					? AssignabilityResult.Assignable
					: AssignabilityResult.NotAssignable;
			case ElementType.Ptr:
			case ElementType.ByRef:
			case ElementType.Pinned:
				// These are assignable only to the same type.
				return default(SigComparer).Equals(source, target) ? AssignabilityResult.Assignable : AssignabilityResult.NotAssignable;
			case ElementType.ValueType:
				// Value types are assignable to themselves and, with boxing, to object, ValueType and their interface types.
				return default(SigComparer).Equals(source, target)
					? AssignabilityResult.Assignable
					: target.ElementType == ElementType.Object || default(SigComparer).Equals(target, typeof(ValueType))
						|| (target.ElementType is ElementType.Class or ElementType.ValueType or ElementType.GenericInst
							&& TypeIsAssignableTo(((ClassOrValueTypeSig) source).TypeDefOrRef, target))
					? AssignabilityResult.AssignableWithBox
					: AssignabilityResult.NotAssignable; 
			case ElementType.SZArray:
			case ElementType.Array:
			case ElementType.Class:
				// Reference types are assignable to themselves, object, their base type and their interface types.
				return target.ElementType == ElementType.Object || default(SigComparer).Equals(source, target)
						|| (target.ElementType is ElementType.Class or ElementType.ValueType or ElementType.GenericInst
							&& TypeIsAssignableTo(((ClassOrValueTypeSig) source).TypeDefOrRef, target))
					? AssignabilityResult.Assignable
					: AssignabilityResult.NotAssignable;
			case ElementType.GenericInst:
				// Generic types use the same rules as value types, but the type arguments must also match.
				var genericInstSig = (GenericInstSig) source;
				return default(SigComparer).Equals(source, target)
					? AssignabilityResult.Assignable
					: target.ElementType == ElementType.Object || 
						(target.ElementType is ElementType.Class or ElementType.ValueType or ElementType.GenericInst
							&& TypeIsAssignableTo(genericInstSig, target))
					? (genericInstSig.GenericType.IsValueType ? AssignabilityResult.AssignableWithBox : AssignabilityResult.Assignable)
					: AssignabilityResult.NotAssignable;
			default:
				// Others are not supported.
				return AssignabilityResult.NotAssignable;
		}
	}
	/// <summary>Determines whether the specified generic type instance is assignable to the target <see cref="TypeSig"/>.</summary>
	private static bool TypeIsAssignableTo(GenericInstSig source, TypeSig target) {
		if (default(SigComparer).Equals(source, target)) return true;
		var sourceDef = source.GenericType.TypeDefOrRef.ResolveTypeDefThrow();
		var baseType = sourceDef.BaseType;
		if (baseType is not null && TypeIsAssignableTo(baseType, target, source.GenericArguments)) return true;
		foreach (var i in sourceDef.Interfaces) {
			if (TypeIsAssignableTo(i.Interface, target, source.GenericArguments)) return true;
		}
		return false;
	}
	/// <summary>Determines whether the specified type is assignable to the target <see cref="TypeSig"/>.</summary>
	private static bool TypeIsAssignableTo(ITypeDefOrRef source, TypeSig target, IList<TypeSig>? genericArguments = null) {
		if (source is TypeSpec typeSpec) {
			if (genericArguments is not null && typeSpec.TypeSig is GenericInstSig genericInstSig) {
				var sig2 = new GenericInstSig(genericInstSig.GenericType,
					genericInstSig.GenericArguments.Select(a => a is GenericVar var ? genericArguments[(int) var.Number] : a).ToArray());
				return TypeIsAssignableTo(sig2, target);
			} else {
				return default(SigComparer).Equals(source, target)
					|| TypeIsAssignableTo(typeSpec.TypeSig, target) != AssignabilityResult.NotAssignable;
			}
		} else {
			if (default(SigComparer).Equals(source, target)) return true;
			var sourceDef = source.ResolveTypeDefThrow();
			var baseType = sourceDef.BaseType;
			if (baseType is not null && TypeIsAssignableTo(baseType, target)) return true;
			foreach (var i in sourceDef.Interfaces) {
				if (TypeIsAssignableTo(i.Interface, target)) return true;
			}
			return false;
		}
	}
}