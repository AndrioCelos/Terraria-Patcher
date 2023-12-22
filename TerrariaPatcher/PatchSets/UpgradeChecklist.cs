#nullable enable

using System;
using System.Collections.Generic;

using Terraria;
using Terraria.ID;

using TerrariaPatcher.Mods;

namespace TerrariaPatcher.PatchSets;

internal class UpgradeChecklist : PatchSet {
	public override string Name => "Upgrade Checklist";
	public override Version Version => new(1, 0);
	public override string Description => "Adds a command that shows which permanent upgrades you have used.";
	public override IReadOnlyCollection<Type>? Dependencies => new[] { typeof(Commands) };

	internal class InitializePatch : MainInitializePatch {
		public static void Prefix() {
			CommandManager.Commands.Add("upgrades", new((_, _, _) => ShowUpgrades(), 0, 0, "", "Shows which permanent upgrades you have used."));
		}
	}

	public static void ShowUpgrades() {
		var playerUpgrades = new List<string>();
		var worldUpgrades = new List<string>();

		var player = Main.LocalPlayer;
		if (player.ateArtisanBread) playerUpgrades.Add($"[i:{ItemID.ArtisanLoaf}]");
		if (player.unlockedBiomeTorches) playerUpgrades.Add($"[i:{ItemID.TorchGodsFavor}]");
		if (player.usedAmbrosia) playerUpgrades.Add($"[i:{ItemID.Ambrosia}]");
		if (player.usedAegisCrystal) playerUpgrades.Add($"[i:{ItemID.AegisCrystal}]");
		if (player.usedArcaneCrystal) playerUpgrades.Add($"[i:{ItemID.ArcaneCrystal}]");
		if (player.usedGalaxyPearl) playerUpgrades.Add($"[i:{ItemID.GalaxyPearl}]");
		if (player.usedGummyWorm) playerUpgrades.Add($"[i:{ItemID.GummyWorm}]");
		if (player.extraAccessory) playerUpgrades.Add($"[i:{ItemID.DemonHeart}]");
		if (player.unlockedSuperCart) playerUpgrades.Add($"[i:{ItemID.MinecartPowerup}]");
		if (player.usedAegisFruit) playerUpgrades.Add($"[i:{ItemID.AegisFruit}]");

		if (NPC.combatBookWasUsed) worldUpgrades.Add($"[i:{ItemID.CombatBook}]");
		if (NPC.combatBookVolumeTwoWasUsed) worldUpgrades.Add($"[i:{ItemID.CombatBookVolumeTwo}]");
		if (NPC.peddlersSatchelWasUsed) worldUpgrades.Add($"[i:{ItemID.PeddlersSatchel}]");

		var playerMessage = playerUpgrades.Count > 0
			? $"Your upgrades: {string.Join(" ", playerUpgrades)}"
			: "You have no permanent upgrades.";
		Main.NewTextMultiline(playerMessage, false, ModManager.AccentColor, -1);
		var worldMessage = worldUpgrades.Count > 0
			? $"World upgrades: {string.Join(" ", worldUpgrades)}"
			: "This world has no permanent upgrades.";
		Main.NewTextMultiline(worldMessage, false, ModManager.AccentColor, -1);
	}
}
