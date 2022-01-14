#nullable enable

using System;

using TerrariaPatcher.Mods;

namespace TerrariaPatcher.PatchSets;

internal class Mods : PatchSet {
	public override string Name => "Mods";
	public override Version Version => new(1, 0);
	public override string Description => "A base patch that can be used by other patches.";

	public override void BeforeApply() {
		ImportType(typeof(ModManager), "Terraria");
	}
}
