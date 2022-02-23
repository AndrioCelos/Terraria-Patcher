#nullable enable

using System;

using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.UI;
using Terraria;

namespace TerrariaPatcher.PatchSets;

internal class LunarPillarNerf : PatchSet {
	public override string Name => "Lunar Pillar Nerf";
	public override Version Version => new(1, 0);
	public override string Description => "Reduces the number of kills needed to break the lunar pillars' shields in expert mode to the same as in normal mode. This works only in single player.";

	public class LunarPillarNerfPatch : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(NPC), $"get_{nameof(NPC.ShieldStrengthTowerMax)}");

		public static bool Prefix(out int __result) {
			__result = NPC.LunarShieldPowerNormal;
			return Main.netMode == 1;  // Will fall through to the original method in a multiplayer client.
		}
	}
}
