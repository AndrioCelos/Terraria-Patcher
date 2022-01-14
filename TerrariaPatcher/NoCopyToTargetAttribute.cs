using System;

namespace TerrariaPatcher;

/// <summary>Specifies that a member of a <see cref="PatchSet"/> or <see cref="Patch"/> should not be copied to the target assembly.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Event | AttributeTargets.Field | AttributeTargets.Method
	| AttributeTargets.Class | AttributeTargets.Struct,
	Inherited = false, AllowMultiple = false)]
internal sealed class NoCopyToTargetAttribute : Attribute { }
