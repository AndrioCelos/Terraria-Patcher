using System;

namespace TerrariaPatcher;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class PatchVersionAttribute : Attribute {
	public Version Version { get; }

	public PatchVersionAttribute(Version version) => this.Version = version ?? throw new ArgumentNullException(nameof(version));
	public PatchVersionAttribute(int major, int minor) : this(new(major, minor)) { }
	public PatchVersionAttribute(int major, int minor, int build) : this(new(major, minor, build)) { }
	public PatchVersionAttribute(int major, int minor, int build, int revision) : this(new(major, minor, build, revision)) { }
}
