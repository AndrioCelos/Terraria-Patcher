#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using dnlib.DotNet;

namespace TerrariaPatcher;

internal class TargetModule {
	public string InputPath { get; }
	public string OutputPath { get; set; }

	public ModuleDef ModuleDef => this.moduleDef ?? throw new InvalidOperationException("Module has not been loaded yet.");

	private ModuleDef? moduleDef;

	public ModuleDef CurrentModuleDef => this.currentModuleDef ?? throw new InvalidOperationException("Module has not been loaded yet.");

	private ModuleDef? currentModuleDef;

	internal IDictionary<Type, TypeDef> AddedTypes { get; } = new Dictionary<Type, TypeDef>();

	public TargetModule(string inputPath, string outputPath) {
		this.InputPath = inputPath ?? throw new ArgumentNullException(nameof(inputPath));
		this.OutputPath = outputPath ?? throw new ArgumentNullException(nameof(outputPath));
	}

	public void Load(IAssemblyResolver assemblyResolver) {
		if (!File.Exists(this.InputPath))
			File.Move(this.OutputPath, this.InputPath);
		this.moduleDef = ModuleDefMD.Load(this.InputPath, new ModuleContext(assemblyResolver));
		this.currentModuleDef = ModuleDefMD.Load(typeof(Program).Module, new ModuleContext(assemblyResolver));
	}

	public void Write() => this.ModuleDef.Write(this.OutputPath);

	internal TypeDef ImportType(Type type) {
		if (type.Module != typeof(Program).Module) throw new ArgumentException("Can import only types from this module.");
		if (!this.AddedTypes.TryGetValue(type, out var typeDef)) {
			return this.ImportType(type, this.currentModuleDef.Import(type));
		}
		return typeDef;
	}
	internal TypeDef ImportType(Type type, ITypeDefOrRef typeDefOrRef) {
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
	internal void ImportType(Type type, TypeDef typeDef) {
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
