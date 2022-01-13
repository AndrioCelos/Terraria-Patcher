#nullable enable

using System;

namespace TerrariaPatcher;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class ReversePatchAttribute : Attribute {
	public PatchTarget PatchTarget { get; }

	public ReversePatchAttribute(Type type, string methodName)
		=> this.PatchTarget = PatchTarget.Create(type, methodName);
	public ReversePatchAttribute(Type type, string methodName, params Type[]? parameterTypes)
		=> this.PatchTarget = PatchTarget.Create(type, methodName, parameterTypes);
	public ReversePatchAttribute(string module, string type, string methodName)
		=> this.PatchTarget = PatchTarget.Create(module, type, methodName);
	public ReversePatchAttribute(string module, string type, string methodName, params Type[]? parameterTypes)
		=> this.PatchTarget = PatchTarget.Create(module, type, methodName, parameterTypes);
}
