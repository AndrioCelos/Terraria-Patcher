#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace TerrariaPatcher.PatchSets;

internal class Watch24Hour : PatchSet {
	public override string Name => "24-Hour Watch";
	public override Version Version => new(1, 1);
	public override string Description => "Changes the watch accessory info display to 24-hour time.";
	public override IReadOnlyCollection<Type> Dependencies => new[] { typeof(InfoAccessoryModifier) };

	internal static string GetWatchString() {
		var ticksFromMidnight = (int) Main.time + (Main.dayTime ? 16200 : 70200);
		var hours = ticksFromMidnight / 3600 % 24;
		var accWatch = Main.player[Main.myPlayer].accWatch;
		int minutes;
		minutes = accWatch == 1 ? 0
			: accWatch == 2 ? ((ticksFromMidnight % 3600 >= 1800) ? 30 : 0)
			: ticksFromMidnight / 60 % 60;
		if (hours == 0 && minutes == 0) hours = 24;
		return $"{hours:00}:{minutes:00}";
	}

	internal class InitializePatch : MainInitializePatch {
		public static void Prefix() => InfoAccessoryModifier.DrawInfoAccessory += DrawInfoAccessory;
		private static void DrawInfoAccessory(int item, ref string title, ref string? text, ref Color colour, ref Color shadowColour) {
			if (item == ItemID.GoldWatch) text = GetWatchString();
		}
	}
}
