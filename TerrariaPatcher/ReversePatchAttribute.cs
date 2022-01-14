#nullable enable

using System;

namespace TerrariaPatcher;

/// <summary>Marks a delegate field as a reverse patch. At run time, the field will point to a method in the target module.</summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class ReversePatchAttribute : Attribute {
	public PatchTarget PatchTarget { get; }

	/// <summary>Initialises a <see cref="ReversePatchAttribute"/> targeting a method in the specified type.</summary>
	/// <param name="type">The type to target. This must be a type in the target module or this module.</param>
	/// <param name="methodName">The name of the method to target.</param>
	public ReversePatchAttribute(Type type, string methodName)
		=> this.PatchTarget = PatchTarget.Create(type, methodName);
	/// <summary>Initialises a <see cref="ReversePatchAttribute"/> targeting a method in the specified type.</summary>
	/// <param name="type">The type to target. This must be a type in the target module or this module.</param>
	/// <param name="methodName">The name of the method to target.</param>
	/// <param name="parameterTypes">The parameter types of the method to patch, excluding the <c>this</c> parameter.</param>
	public ReversePatchAttribute(Type type, string methodName, params Type[]? parameterTypes)
		=> this.PatchTarget = PatchTarget.Create(type, methodName, parameterTypes);
	/// <summary>Initialises a <see cref="ReversePatchAttribute"/> targeting a method in the specified type.</summary>
	/// <param name="module">The name of the module containing the type to target.</param>
	/// <param name="type">The full name of the type to target.</param>
	/// <param name="methodName">The name of the method to target.</param>
	public ReversePatchAttribute(string module, string type, string methodName)
		=> this.PatchTarget = PatchTarget.Create(module, type, methodName);
	/// <summary>Initialises a <see cref="ReversePatchAttribute"/> targeting a method in the specified type.</summary>
	/// <param name="module">The name of the module containing the type to target.</param>
	/// <param name="type">The full name of the type to target.</param>
	/// <param name="methodName">The name of the method to target.</param>
	/// <param name="parameterTypes">The parameter types of the method to patch, excluding the <c>this</c> parameter.</param>
	public ReversePatchAttribute(string module, string type, string methodName, params Type[]? parameterTypes)
		=> this.PatchTarget = PatchTarget.Create(module, type, methodName, parameterTypes);
}
