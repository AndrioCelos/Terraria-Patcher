#nullable enable

using System;
using System.Collections.Generic;

using Terraria;
using Terraria.ID;

using TerrariaPatcher.Mods;

namespace TerrariaPatcher.PatchSets;

internal class SundialHint : PatchSet {
	public override string Name => "Sundial Hint";
	public override Version Version => new(1, 0);
	public override string Description => "Adds a message when clicking on an enchanted sundial during the cooldown in single player.";
	public override IReadOnlyCollection<Type>? Dependencies => new[] { typeof(Mods) };

	internal class SundialHintPatch : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(Player), "TileInteractionsUse");

		public static void Postfix(Player __instance, int myX, int myY) {
			if (__instance.tileInteractAttempted && __instance.releaseUseTile && __instance.tileInteractionHappened
				&& Main.netMode == 0 && !Main.fastForwardTime && Main.tile[myX, myY].type == TileID.Sundial && Main.sundialCooldown != 0) {
				Main.NewTextMultiline($"{Main.sundialCooldown} {(Main.sundialCooldown == 1 ? "day remains" : "days remain")} until this can be used again.", false, ModManager.AccentColor);
			}
		}
	}
}
