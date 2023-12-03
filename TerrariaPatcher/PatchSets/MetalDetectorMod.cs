#nullable enable

using System;
using Terraria.ID;
using Terraria;

namespace TerrariaPatcher.PatchSets;

internal class MetalDetectorMod : PatchSet {
	public override string Name => "Metal Detector Adjustments";
	public override Version Version => new(1, 1);
	public override string Description => "Changes the Metal Detector to prioritise life crystals just below chests.";

	public class MetalDetectorPatch : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(Main), "SetTileValue");

		public static void Postfix() {
			var newPriority = (short) (Main.tileOreFinderPriority[TileID.Containers] - 1);
			Main.tileOreFinderPriority[TileID.Heart] = newPriority;
			Main.tileOreFinderPriority[TileID.LifeCrystalBoulder] = newPriority;
			Main.tileOreFinderPriority[TileID.ManaCrystal] = newPriority;
		}
	}
}
