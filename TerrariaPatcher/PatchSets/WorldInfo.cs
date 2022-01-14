#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

using Terraria;
using Terraria.GameContent.Events;

using TerrariaPatcher.Mods;

namespace TerrariaPatcher.PatchSets;

internal class WorldInfo : PatchSet {
	public override string Name => "World Info";
	public override Version Version => new(1, 0);
	public override string Description => "Shows world information upon entering and with a command.";
	public override IReadOnlyCollection<Type>? Dependencies => new[] { typeof(Commands) };

	private static int lastWorldID;

	internal class InitializePatch : MainInitializePatch {
		public static void Prefix() {
			CommandManager.Commands.Add("world", args => ShowWorldInfo(false));
			Player.Hooks.OnEnterWorld += p => {
				if (Main.worldID != lastWorldID) {
					lastWorldID = Main.worldID;
					ShowWorldInfo(true);
				}
			};
		}
	}

	public static void ShowWorldInfo(bool entering) {
		Main.NewTextMultiline($"{(entering ? "Entering" : "This is")} [c/FFFFFF:{Main.worldName.Replace("]", "\\]")}] ([c/FFFFFF:{Main.ActiveWorldFileData.UniqueId}] / [c/FFFFFF:{Main.worldID}])", false, ModManager.AccentColor, -1);
		var sizeText = Main.maxTilesX == 8400 && Main.maxTilesY == 2400 ? "[c/32CD32:Large]"
			: Main.maxTilesX >= 8400 || Main.maxTilesY >= 2400 ? "[c/32CD32:Large+]"
			: Main.maxTilesX == 6400 && Main.maxTilesY == 1800 ? "[c/19E698:Medium]"
			: Main.maxTilesX >= 6400 || Main.maxTilesY >= 1800 ? "[c/32CD32:Medium+]"
			: Main.maxTilesX == 4200 && Main.maxTilesY == 1200 ? "[c/00FFFF:Small]"
			: Main.maxTilesX >= 4200 || Main.maxTilesY >= 1200 ? "[c/19E698:Small+]"
			: "[c/00FFFF:Tiny+]";

		var secrets = new List<string>();
		if (Main.tenthAnniversaryWorld) secrets.Add("10th Anniversary");
		if (Main.dontStarveWorld) secrets.Add("Don't Starve");
		if (Main.drunkWorld) secrets.Add("Drunk World");
		if (Main.getGoodWorld) secrets.Add("For the Worthy");
		if (Main.notTheBeesWorld) secrets.Add("Not the Bees!");
		var secretsText = secrets.Count > 0 ? $"  Secret: [c/FFFFFF:{string.Join(", ", secrets)}]" : "";

		var difficultyText = Main.GameModeInfo.IsJourneyMode ? "[c/FF78BB:Journey]"
			: Main.masterMode ? "[c/FF261A:Master]"
			: Main.expertMode ? "[c/FF9900:Expert]"
			: "[c/FFFFFF:Classic]";
		var stageText = Main.hardMode ? "[c/36D8D8:Hardmode]" : "[c/FFFFFF:Pre-hardmode]";
		var evilText = WorldGen.crimson ? "[c/C45858:Crimson]" : "[c/9370DB:Corruption]";
		Main.NewTextMultiline($"Size: {sizeText}  Difficulty: {difficultyText}  Stage: {stageText}  Evil: {evilText}{secretsText}", false, ModManager.AccentColor, -1);

		var list = new List<(string longTag, string shortTag)>();
		if (NPC.downedSlimeKing)
			list.Add(("[c/AF4BFF:King Slime]", "[c/AF4BFF:King Slime]"));
		if (NPC.downedBoss1)
			list.Add(("[c/AF4BFF:Eye of Cthulhu]", "[c/AF4BFF:Eye]"));
		if (NPC.downedBoss2)
			list.Add(WorldGen.crimson ? ("[c/AF4BFF:Brain of Cthulhu]", "[c/AF4BFF:Brain]") : ("[c/AF4BFF:Eater of Worlds]", "[c/AF4BFF:Eater]"));
		if (NPC.downedBoss3)
			list.Add(("[c/AF4BFF:Skeletron]", "[c/AF4BFF:Skeletron]"));
		if (NPC.downedQueenBee)
			list.Add(("[c/AF4BFF:Queen Bee]", "[c/AF4BFF:Bee]"));
		if (NPC.downedDeerclops)
			list.Add(("[c/AF4BFF:Deerclops]", "[c/AF4BFF:Deerclops]"));
		if (Main.hardMode)
			list.Add(("[c/AF4BFF:Wall of Flesh]", "[c/AF4BFF:Wall]"));
		if (NPC.downedQueenSlime)
			list.Add(("[c/AF4BFF:Queen Slime]", "[c/AF4BFF:Queen Slime]"));

		// Hardmode bosses that require other bosses to be defeated before they are accessible suppress listing of those bosses.
		// e.g. we won't list the mechanical bosses if Plantera has been killed.
		if (!NPC.downedAncientCultist) {
			if (NPC.downedGolemBoss)
				list.Add(("[c/AF4BFF:Golem]", "[c/AF4BFF:Golem]"));
			else if (NPC.downedPlantBoss)
				list.Add(("[c/AF4BFF:Plantera]", "[c/AF4BFF:Plantera]"));
			else {
				if (NPC.downedMechBoss1)
					list.Add(("[c/AF4BFF:The Destroyer]", "[c/AF4BFF:Destroyer]"));
				if (NPC.downedMechBoss2)
					list.Add(("[c/AF4BFF:The Twins]", "[c/AF4BFF:Twins]"));
				if (NPC.downedMechBoss3)
					list.Add(("[c/AF4BFF:Skeletron Prime]", "[c/AF4BFF:Prime]"));
			}
		}
		if (NPC.downedFishron)
			list.Add(("[c/AF4BFF:Duke Fishron]", "[c/AF4BFF:Fishron]"));
		if (NPC.downedEmpressOfLight)
			list.Add(("[c/AF4BFF:Empress of Light]", "[c/AF4BFF:Empress]"));
		if (NPC.downedAncientCultist)
			list.Add(("[c/AF4BFF:Lunatic Cultist]", "[c/AF4BFF:Cultist]"));
		if (NPC.downedMoonlord)
			list.Add(("[c/AF4BFF:Moon Lord]", "[c/AF4BFF:Moon Lord]"));

		if (NPC.downedGoblins)
			list.Add(("[c/32FF82:Goblins]", "[c/32FF82:Goblins]"));
		if (NPC.downedFrost)
			list.Add(("[c/32FF82:Frost Legion]", "[c/32FF82:Frost]"));
		if (NPC.downedPirates)
			list.Add(("[c/32FF82:Pirates]", "[c/32FF82:Pirates]"));
		if (NPC.downedMartians)
			list.Add(("[c/32FF82:Martians]", "[c/32FF82:Martians]"));
		var oldOnesTier = DD2Event.DownedInvasionT3 ? 3 : DD2Event.DownedInvasionT2 ? 2 : DD2Event.DownedInvasionT1 ? 1 : 0;
		if (oldOnesTier > 0) {
			var text = $"[c/32FF82:Old Ones {oldOnesTier}]";
			list.Add((text, text));
		}
		if (list.Count == 0) {
			Main.NewTextMultiline("Defeated: None", false, ModManager.AccentColor, -1);
			return;
		}
		if (list.Count <= 6)
			Main.NewTextMultiline("Defeated: " + string.Join(", ", from l in list select l.longTag), false, ModManager.AccentColor, -1);
		else
			Main.NewTextMultiline("Defeated: " + string.Join(", ", from l in list select l.shortTag), false, ModManager.AccentColor, -1);
	}
}
