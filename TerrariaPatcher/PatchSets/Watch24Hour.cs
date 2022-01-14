#nullable enable

using System;

using dnlib.DotNet;

using Terraria;

namespace TerrariaPatcher.PatchSets;

internal class Watch24Hour : PatchSet {
	public override string Name => "24-Hour Watch";
	public override Version Version => new(1, 0);
	public override string Description => "Changes the watch accessory info display to 24-hour time.";

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

	internal class DrawInfoAccsPatch : Patch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(Main), "DrawInfoAccs");

		public override void PatchMethodBody(MethodDef method) {
			// Replace the code after `text3 = Lang.inter[95].Value` that builds the time string.
			var instructions = method.Body.Instructions;
			for (int i = 3; i < instructions.Count; i++) {
				if (instructions[i - 3].IsConstant(95)
					&& instructions[i].IsStloc()) {
					i++;
					var foundColon = false;
					while (true) {
						if (!foundColon) {
							if (instructions[i].IsConstant(":"))
								foundColon = true;
						} else if (instructions[i].IsStloc()) {
							instructions.Insert(i, Call(Watch24Hour.GetWatchString));
							return;
						}
						instructions.RemoveAt(i);
					}
				}
			}
			throw new ArgumentException("Couldn't find code to replace.");
		}
	}
}
