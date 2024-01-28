#nullable enable

using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace TerrariaPatcher.PatchSets;

internal class SlimeRainTallyCounter : PatchSet {
	public override string Name => "Slime Rain Tally Counter";
	public override Version Version => new(1, 0);
	public override string Description => "Shows the number of slimes killed during Slime Rain on the tally counter.";
	public override IReadOnlyCollection<Type> Dependencies => new[] { typeof(InfoAccessoryModifier) };

	private static bool gradientCycle;
	private static int startingKillCount;

	public static readonly Color GREEN = new(0, 220, 40);
	public static readonly Color BLUE = new(0, 80, 255);
	public static readonly Color PINK = new(250, 30, 90);
	public static readonly Color PURPLE = new(200, 0, 255);

	private static int lastWorldID;

	internal static string? GetCounterDisplayString(ref Color colour) {
		if (!Main.slimeRain) return null;

		// Cycle colours between green, blue, pink and purple to match the colour of the slimes that appear.
		if (Main.mouseTextColor == byte.MaxValue)
			gradientCycle = !gradientCycle;

		var gradientValue = (Main.mouseTextColorChange < 0 ? 255 - Main.mouseTextColor : 65 - 190 + Main.mouseTextColor) + (gradientCycle ? 130 : 0);  // 260 frame period
		colour = gradientValue switch {
			< 65 => Color.Lerp(GREEN, BLUE, gradientValue / 65f),
			< 130 => Color.Lerp(BLUE, PINK, (gradientValue - 65) / 65f),
			< 195 => Color.Lerp(PINK, PURPLE, (gradientValue - 130) / 65f),
			_ => Color.Lerp(PURPLE, GREEN, (gradientValue - 195) / 65f)
		};

		if (startingKillCount <= 0) {
			// If we joined a server during a slime rain, the best we can do is show the maximum value.
			startingKillCount = GetTotalSlimeKillCount();
		}

		var killsRequired = NPC.downedSlimeKing ? 75 : 150;
		var killsLeft = Main.netMode == 0
			? killsRequired - Main.slimeRainKillCount
			: startingKillCount + killsRequired - GetTotalSlimeKillCount();

		if (Main.netMode != 0 && killsLeft <= 0) {
			// When the required kill count is reached and King Slime appears, it can be summoned again if another 3/2 the required number of slimes are killed.
			killsLeft = killsRequired * 3 / 2;
			startingKillCount = GetTotalSlimeKillCount() - killsRequired / 2;
		}

		return $"Slimes left: {killsLeft}";
	}

	private static int GetTotalSlimeKillCount() => NPC.killCount[69] + NPC.killCount[119] + NPC.killCount[151] + NPC.killCount[155];

	internal class InitializePatch : MainInitializePatch {
		public static void Prefix() {
			InfoAccessoryModifier.DrawInfoAccessory += DrawInfoAccessory;
			Player.Hooks.OnEnterWorld += p => {
				if (Main.worldID != lastWorldID) {
					lastWorldID = Main.worldID;
					startingKillCount = 0;
				}
			};
		}

		private static void DrawInfoAccessory(int item, ref string title, ref string? text, ref Color colour, ref Color shadowColour) {
			if (item == ItemID.TallyCounter && text == null) text = GetCounterDisplayString(ref colour);
		}
	}

	internal class StartSlimeRainPatch : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(Main.StartSlimeRain);

		public static void Prefix() {
			if (!Main.slimeRain) startingKillCount = GetTotalSlimeKillCount();
		}
	}
}
