#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using dnlib.DotNet;

namespace TerrariaPatcher;

/// <summary>Encapsulates a <see cref="ModuleDef"/> that can be patched.</summary>
internal class TargetModule {
	/// <summary>The path that the original module should be loaded from.</summary>
	public string InputPath { get; }
	/// <summary>The path that the patched module should be written to.</summary>
	public string OutputPath { get; set; }
	/// <summary>Whether this module has been modified.</summary>
	public bool Modified { get; set; }

	/// <summary>The <see cref="ModuleDef"/> representing the target module.</summary>
	/// <exception cref="InvalidOperationException">The module has not been loaded yet.</exception>
	public ModuleDef ModuleDef => this.moduleDef ?? throw new InvalidOperationException("Module has not been loaded yet.");

	private ModuleDef? moduleDef;

	/// <summary>The <see cref="ModuleDef"/> representing a copy of this module.</summary>
	/// <exception cref="InvalidOperationException">The module has not been loaded yet.</exception>
	// This is used to move code from this module referenced by mods to the target module.
	public ModuleDef CurrentModuleDef => this.currentModuleDef ?? throw new InvalidOperationException("Module has not been loaded yet.");

	private ModuleDef? currentModuleDef;

	/// <summary>A collection of types from this module that have been copied to the target module.</summary>
	internal IDictionary<Type, TypeDef> AddedTypes { get; } = new Dictionary<Type, TypeDef>();

	public TargetModule(string inputPath) : this(inputPath, Path.Combine(Path.GetDirectoryName(inputPath),
		$"{Path.GetFileNameWithoutExtension(inputPath)}.patched{Path.GetExtension(inputPath)}")) { }
	public TargetModule(string inputPath, string outputPath) {
		this.InputPath = inputPath ?? throw new ArgumentNullException(nameof(inputPath));
		this.OutputPath = outputPath ?? throw new ArgumentNullException(nameof(outputPath));
	}

	/// <summary>Loads the target module.</summary>
	public void Load() {
		this.moduleDef = ModuleDefMD.Load(this.InputPath, new ModuleContext(Program.AssemblyResolver));
		this.currentModuleDef = ModuleDefMD.Load(typeof(Program).Module, new ModuleContext(Program.AssemblyResolver));
		this.AddedTypes.Clear();
	}

	/// <summary>Writes the modified target module to the <see cref="OutputPath"/>.</summary>
	public void Write() => this.ModuleDef.Write(this.OutputPath);

	/// <summary>Ensures that the specified type and its base types are copied from this module to the target module and returns the copy as a <see cref="TypeDef"/>.</summary>
	internal TypeDef ImportType(Type type) => type.Module != typeof(Program).Module
			? throw new ArgumentException("Can import only types from this module.")
			: this.AddedTypes.TryGetValue(type, out var typeDef)
			? typeDef
			: this.ImportType(type, this.CurrentModuleDef.Import(type));
	/// <summary>Copies the specified type and its base types from this module to the target module.</summary>
	private TypeDef ImportType(Type type, ITypeDefOrRef typeDefOrRef) {
		var typeDef = typeDefOrRef switch {
			TypeDef typeDef2 => typeDef2,
			TypeRef typeRef => typeRef.ResolveThrow(this.currentModuleDef),
			_ => throw new InvalidOperationException("Can't resolve added type"),
		};

		if (type.BaseType is not null && type.BaseType.Module == type.Module)
			this.ImportType(type.BaseType, typeDef.BaseType);
		foreach (var i in type.GetInterfaces()) {
			if (i.Module == type.Module)
				this.ImportType(i);
		}

		this.ImportType(type, typeDef);
		return typeDef;
	}
	/// <summary>Copies the specified type and its base types from this module to the target module.</summary>
	private void ImportType(Type type, TypeDef typeDef) {
		if (typeDef.Module == this.ModuleDef) return;
		if (typeDef.Module != this.currentModuleDef) throw new ArgumentException("Can import only types from this module.");
		this.currentModuleDef.Types.Remove(typeDef);
		if (this.ModuleDef.Types.Any(t => t.FullName == typeDef.FullName))
			typeDef.Name += Guid.NewGuid().ToString();
		this.ModuleDef.Types.Add(typeDef);
		this.AddedTypes[type] = typeDef;
	}

	public override string ToString() => this.moduleDef?.ToString() ?? $"{{{this.InputPath}}}";
}
