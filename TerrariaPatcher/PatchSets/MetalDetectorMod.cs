#nullable enable

using System;
using Terraria.ID;
using Terraria;

namespace TerrariaPatcher.PatchSets;

internal class MetalDetectorMod : PatchSet {
	public override string Name => "Metal Detector Adjustments";
	public override Version Version => new(1, 0);
	public override string Description => "Changes the Metal Detector to prioritise life crystals just below chests.";

	public class MetalDetectorPatch : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(Main), "SetTileValue");

		public static void Postfix()
			=> Main.tileOreFinderPriority[TileID.Heart] = (short) (Main.tileOreFinderPriority[TileID.Containers] - 1);
	}
}
