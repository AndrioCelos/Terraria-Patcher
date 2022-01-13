#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

using dnlib.DotNet;

namespace TerrariaPatcher;

public class PatchTarget {
	private readonly string toString;
	private readonly Func<IEnumerable<MethodDef>> func;

	public PatchTarget(string toString, Func<MethodDef> func)
		: this(toString, () => new[] { func() }) { }
	public PatchTarget(string toString, Func<IEnumerable<MethodDef>> func) {
		this.toString = toString ?? throw new ArgumentNullException(nameof(toString));
		this.func = func ?? throw new ArgumentNullException(nameof(func));
	}

	public IEnumerable<MethodDef> GetMethodDefs() => this.func();
	public override string ToString() => this.toString;

	public static PatchTarget Create(Func<MethodDef> method)
		=> new(method.ToString(), method);
	public static PatchTarget Create(Func<IEnumerable<MethodDef>> methods)
		=> new(methods.ToString(), methods);
	public static PatchTarget Create(Delegate method)
		=> new(method.ToString(), () => Program.GetTargetModule(method.Method.Module).ModuleDef.Import(method.Method).ResolveMethodDefThrow());
	public static PatchTarget Create(Type type, Func<TypeDef, MethodDef> method)
		=> new(method.ToString(), () => method(GetTypeDef(type)));
	public static PatchTarget Create(Type type, Func<TypeDef, IEnumerable<MethodDef>> methods)
		=> new(methods.ToString(), () => methods(GetTypeDef(type)));
	public static PatchTarget Create(Type type, string methodName)
		=> new($"{type}.{methodName}",
			() => GetMethodDef(GetTypeDef(type), methodName));
	public static PatchTarget Create(Type type, string methodName, params Type[]? parameterTypes)
		=> new($"{type}.{methodName}({string.Join<Type>(", ", parameterTypes)})",
			() => GetMethodDef(GetTypeDef(type), methodName, parameterTypes));
	public static PatchTarget Create(string module, string type, Func<TypeDef, MethodDef> method)
		=> new(method.ToString(), () => method(GetTypeDef(module, type)));
	public static PatchTarget Create(string module, string type, Func<TypeDef, IEnumerable<MethodDef>> methods)
		=> new(methods.ToString(), () => methods(GetTypeDef(module, type)));
	public static PatchTarget Create(string module, string type, string methodName)
		=> new($"{type}.{methodName}",
			() => GetMethodDef(GetTypeDef(module, type), methodName));
	public static PatchTarget Create(string module, string type, string methodName, params Type[]? parameterTypes)
		=> new($"{type}.{methodName}({string.Join<Type>(", ", parameterTypes)})",
			() => GetMethodDef(GetTypeDef(module, type), methodName, parameterTypes));

	public static PatchTarget Constructor(Type type)
		=> new($"{type}..ctor()",
			() => GetTypeDef(type).FindDefaultConstructor());
	public static PatchTarget Constructor(Type type, params Type[]? parameterTypes)
		=> Create(type, ".ctor", parameterTypes);
	public static PatchTarget Constructor(string module, string type)
		=> new($"{type}..ctor()",
			() => GetTypeDef(module, type).FindDefaultConstructor());
	public static PatchTarget Constructor(string module, string type, params Type[]? parameterTypes)
		=> Create(module, type, ".ctor", parameterTypes);

	public static PatchTarget StaticConstructor(Type type)
		=> new($"{type}..cctor()", () => GetTypeDef(type).FindStaticConstructor());
	public static PatchTarget StaticConstructor(string module, string type)
		=> new($"{type}..cctor()", () => GetTypeDef(module, type).FindStaticConstructor());

	private static TypeDef GetTypeDef(Type type) => Program.GetTargetModule(type.Module).ModuleDef.Import(type).ResolveTypeDefThrow();
	private static TypeDef GetTypeDef(string module, string type) {
		var tokens = type.Split(new[] { '+', '/' });
		var typeDef = Program.GetTargetModule(module).ModuleDef.Types.SingleOrDefault(t => t.FullName == tokens[0])
			?? throw new ArgumentException($"No such type: {tokens[0]}");
		for (int i = 1; i < tokens.Length; i++) {
			typeDef = typeDef.NestedTypes.SingleOrDefault(t => t.Name == tokens[i])
				?? throw new ArgumentException($"No such nested type: {tokens[i]}");
		}
		return typeDef;
	}
	private static MethodDef GetMethodDef(TypeDef typeDef, string methodName)
		=> typeDef.Methods.SingleOrDefault(m => m.Name == methodName);
	private static MethodDef GetMethodDef(TypeDef typeDef, string methodName, IList<Type>? parameterTypes) {
		parameterTypes ??= Array.Empty<Type>();
		return typeDef.Methods.SingleOrDefault(m => m.Name == methodName
			&& m.Parameters.Count(p => !p.IsHiddenThisParameter) == parameterTypes.Count
			&& m.Parameters.Where(p => !p.IsHiddenThisParameter)
				.Select((p, i) => default(SigComparer).Equals(parameterTypes[i], p.Type)).All(b => b));
	}
}
