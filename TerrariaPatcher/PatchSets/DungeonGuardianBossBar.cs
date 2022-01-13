#nullable enable

using System;
using System.Collections;

using Terraria.GameContent.UI.BigProgressBar;
using Terraria.ID;

namespace TerrariaPatcher.PatchSets;

internal class DungeonGuardianBossBar : PatchSet {
	public override string Name => "Dungeon Guardian Boss Bar";
	public override Version Version => new(1, 0);
	public override string Description => "Enables a boss bar for Dungeon Guardians.";

	internal class DungeonGuardianBossBarPatch : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Constructor(typeof(BigProgressBarSystem));
		public static void Postfix(IDictionary ____bossBarsByNpcNetId) => ____bossBarsByNpcNetId.Remove((int) NPCID.DungeonGuardian);
	}
}
