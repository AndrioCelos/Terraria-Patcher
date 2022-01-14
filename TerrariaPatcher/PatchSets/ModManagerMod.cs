#nullable enable

using System;

using TerrariaPatcher.Mods;

namespace TerrariaPatcher.PatchSets;

internal class ModManagerMod : PatchSet {
	public override string Name => "Mod Manager";
	public override Version Version => new(1, 0);
	public override string Description => "Provides code used by other mods.";

	public override void BeforeApply() {
		ImportType(typeof(ModManager), "Terraria");
	}
}
