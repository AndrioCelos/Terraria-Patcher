#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

using dnlib.DotNet;

namespace TerrariaPatcher;

/// <summary>Specifies the methods that should be patched by a <see cref="Patch"/>.</summary>
public class PatchTarget {
	private readonly string toString;
	private readonly Func<IEnumerable<MethodDef>> func;

	/// <summary>Initialises a <see cref="PatchTarget"/> to select a single method.</summary>
	/// <param name="toString">A string representation of the <see cref="PatchTarget"/>.</param>
	/// <param name="func">A function that returns the method to patch. Do not use a lambda or other anonymous method here.</param>
	public PatchTarget(string toString, Func<MethodDef> func)
		: this(toString, () => new[] { func() }) { }
	/// <summary>Initialises a <see cref="PatchTarget"/> to select zero or more methods.</summary>
	/// <param name="toString">A string representation of the <see cref="PatchTarget"/>.</param>
	/// <param name="func">A function that returns the methods to patch. Do not use a lambda or other anonymous method here.</param>
	public PatchTarget(string toString, Func<IEnumerable<MethodDef>> func) {
		this.toString = toString ?? throw new ArgumentNullException(nameof(toString));
		this.func = func ?? throw new ArgumentNullException(nameof(func));
	}

	/// <summary>Returns the sequence of methods that should be patched.</summary>
	public IEnumerable<MethodDef> GetMethodDefs() => this.func();
	public override string ToString() => this.toString;

	/// <summary>Creates a <see cref="PatchTarget"/> from the specified method selector.</summary>
	public static PatchTarget Create(Func<MethodDef> method)
		=> new(method.ToString(), method);
	/// <summary>Creates a <see cref="PatchTarget"/> from the specified method sequence selector.</summary>
	public static PatchTarget Create(Func<IEnumerable<MethodDef>> methods)
		=> new(methods.ToString(), methods);
	/// <summary>Creates a <see cref="PatchTarget"/> targeting the specified method.</summary>
	/// <param name="method">The method to target. This must be a static method in the target module or this module.</param>
	public static PatchTarget Create(Delegate method)
		=> new(method.ToString(), () => Program.GetTargetModule(method.Method.Module).ModuleDef.Import(method.Method).ResolveMethodDefThrow());
	/// <summary>Creates a <see cref="PatchTarget"/> targeting a method in the specified type.</summary>
	/// <param name="type">The type to target. This must be a type in the target module or this module.</param>
	/// <param name="method">A delegate that obtains the method to patch from the resolved <see cref="TypeDef"/>.</param>
	public static PatchTarget Create(Type type, Func<TypeDef, MethodDef> method)
		=> new(method.ToString(), () => method(GetTypeDef(type)));
	/// <summary>Creates a <see cref="PatchTarget"/> targeting a method in the specified type.</summary>
	/// <param name="type">The type to target. This must be a type in the target module or this module.</param>
	/// <param name="methods">A delegate that obtains the methods to patch from the resolved <see cref="TypeDef"/>.</param>
	public static PatchTarget Create(Type type, Func<TypeDef, IEnumerable<MethodDef>> methods)
		=> new(methods.ToString(), () => methods(GetTypeDef(type)));
	/// <summary>Creates a <see cref="PatchTarget"/> targeting a method in the specified type.</summary>
	/// <param name="type">The type to target. This must be a type in the target module or this module.</param>
	/// <param name="methodName">The name of the method to patch.</param>
	public static PatchTarget Create(Type type, string methodName)
		=> new($"{type}.{methodName}",
			() => GetMethodDef(GetTypeDef(type), methodName));
	/// <summary>Creates a <see cref="PatchTarget"/> targeting a method in the specified type.</summary>
	/// <param name="type">The type to target. This must be a type in the target module or this module.</param>
	/// <param name="methodName">The name of the method to patch.</param>
	/// <param name="parameterTypes">The parameter types of the method to patch, excluding the <c>this</c> parameter.</param>
	public static PatchTarget Create(Type type, string methodName, params Type[]? parameterTypes)
		=> new($"{type}.{methodName}({string.Join<Type>(", ", parameterTypes)})",
			() => GetMethodDef(GetTypeDef(type), methodName, parameterTypes));
	/// <summary>Creates a <see cref="PatchTarget"/> targeting a method in the specified type.</summary>
	/// <param name="module">The name of the module containing the type to target.</param>
	/// <param name="type">The full name of the type to target.</param>
	/// <param name="method">The name of the method to patch.</param>
	/// <param name="method">A delegate that obtains the method to patch from the resolved <see cref="TypeDef"/>.</param>
	public static PatchTarget Create(string module, string type, Func<TypeDef, MethodDef> method)
		=> new(method.ToString(), () => method(GetTypeDef(module, type)));
	/// <summary>Creates a <see cref="PatchTarget"/> targeting a method in the specified type.</summary>
	/// <param name="module">The name of the module containing the type to target.</param>
	/// <param name="type">The full name of the type to target.</param>
	/// <param name="methods">A delegate that obtains the methods to patch from the resolved <see cref="TypeDef"/>.</param>
	public static PatchTarget Create(string module, string type, Func<TypeDef, IEnumerable<MethodDef>> methods)
		=> new(methods.ToString(), () => methods(GetTypeDef(module, type)));
	/// <summary>Creates a <see cref="PatchTarget"/> targeting a method in the specified type.</summary>
	/// <param name="module">The name of the module containing the type to target.</param>
	/// <param name="type">The full name of the type to target.</param>
	/// <param name="methodName">The name of the method to patch.</param>
	public static PatchTarget Create(string module, string type, string methodName)
		=> new($"{type}.{methodName}",
			() => GetMethodDef(GetTypeDef(module, type), methodName));
	/// <summary>Creates a <see cref="PatchTarget"/> targeting a method in the specified type.</summary>
	/// <param name="module">The name of the module containing the type to target.</param>
	/// <param name="type">The full name of the type to target.</param>
	/// <param name="methodName">The name of the method to patch.</param>
	/// <param name="parameterTypes">The parameter types of the method to patch, excluding the <c>this</c> parameter.</param>
	public static PatchTarget Create(string module, string type, string methodName, params Type[]? parameterTypes)
		=> new($"{type}.{methodName}({string.Join<Type>(", ", parameterTypes)})",
			() => GetMethodDef(GetTypeDef(module, type), methodName, parameterTypes));

	/// <summary>Creates a <see cref="PatchTarget"/> targeting the default constructor of the specified type.</summary>
	/// <param name="type">The type to target. This must be a type in the target module or this module.</param>
	public static PatchTarget Constructor(Type type)
		=> new($"{type}..ctor()",
			() => GetTypeDef(type).FindDefaultConstructor());
	/// <summary>Creates a <see cref="PatchTarget"/> targeting a constructor of the specified type.</summary>
	/// <param name="type">The type to target. This must be a type in the target module or this module.</param>
	/// <param name="parameterTypes">The parameter types of the method to patch, excluding the <c>this</c> parameter.</param>
	public static PatchTarget Constructor(Type type, params Type[]? parameterTypes)
		=> Create(type, ".ctor", parameterTypes);
	/// <summary>Creates a <see cref="PatchTarget"/> targeting the default constructor of the specified type.</summary>
	/// <param name="module">The name of the module containing the type to target.</param>
	/// <param name="type">The full name of the type to target.</param>
	public static PatchTarget Constructor(string module, string type)
		=> new($"{type}..ctor()",
			() => GetTypeDef(module, type).FindDefaultConstructor());
	/// <summary>Creates a <see cref="PatchTarget"/> targeting a constructor of the specified type.</summary>
	/// <param name="module">The name of the module containing the type to target.</param>
	/// <param name="type">The full name of the type to target.</param>
	/// <param name="parameterTypes">The parameter types of the method to patch, excluding the <c>this</c> parameter.</param>
	public static PatchTarget Constructor(string module, string type, params Type[]? parameterTypes)
		=> Create(module, type, ".ctor", parameterTypes);

	/// <summary>Creates a <see cref="PatchTarget"/> targeting the static constructor of the specified type.</summary>
	/// <param name="type">The type to target. This must be a type in the target module or this module.</param>
	public static PatchTarget StaticConstructor(Type type)
		=> new($"{type}..cctor()", () => GetTypeDef(type).FindStaticConstructor());
	/// <summary>Creates a <see cref="PatchTarget"/> targeting the static constructor of the specified type.</summary>
	/// <param name="module">The name of the module containing the type to target.</param>
	/// <param name="type">The full name of the type to target.</param>
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
