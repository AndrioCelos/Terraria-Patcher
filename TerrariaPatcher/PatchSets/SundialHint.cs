#nullable enable

using System;
using System.Collections.Generic;

using Terraria;
using Terraria.ID;

using TerrariaPatcher.Mods;

namespace TerrariaPatcher.PatchSets;

internal class SundialHint : PatchSet {
	public override string Name => "Sundial Hint";
	public override Version Version => new(1, 1);
	public override string Description => "Adds a message when clicking on an enchanted sundial or moondial during the cooldown in single player.";
	public override IReadOnlyCollection<Type>? Dependencies => new[] { typeof(ModManagerMod) };

	internal class SundialHintPatch : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(Player), "TileInteractionsUse");

		public static void Postfix(Player __instance, int myX, int myY) {
			if (__instance.tileInteractAttempted && __instance.releaseUseTile && __instance.tileInteractionHappened
				&& Main.netMode == 0 && !Main.fastForwardTimeToDawn && Main.tile[myX, myY].type == TileID.Sundial && Main.sundialCooldown != 0) {
				Main.NewTextMultiline($"{Main.sundialCooldown} {(Main.sundialCooldown == 1 ? "day remains" : "days remain")} until this can be used again.", false, ModManager.AccentColor);
			} else if (__instance.tileInteractAttempted && __instance.releaseUseTile && __instance.tileInteractionHappened
				&& Main.netMode == 0 && !Main.fastForwardTimeToDusk && Main.tile[myX, myY].type == TileID.Moondial && Main.moondialCooldown != 0) {
				Main.NewTextMultiline($"{Main.moondialCooldown} {(Main.moondialCooldown == 1 ? "night remains" : "nights remain")} until this can be used again.", false, ModManager.AccentColor);
			}
		}
	}
}
