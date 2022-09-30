#nullable enable

using System;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

using Terraria;

namespace TerrariaPatcher.PatchSets;

internal class InvasionProgress : PatchSet {
	public override string Name => "Invasion Progress";
	public override Version Version => new(1, 1);
	public override string Description => "Changes the invasion progress text to show the exact number of points left, and time remaining in Old One's Army intermissions.";

	internal class InvasionProgressPatch : Patch {
		public override PatchTarget TargetMethod => PatchTarget.Create(Main.DrawInvasionProgress);

		internal static string GetProgressString() {
			if (Main.invasionProgressIcon == 3 && Terraria.GameContent.Events.DD2Event.EnemySpawningIsOnHold)
				return Lang.LocalizedDuration(new TimeSpan(0, 0, 0, 0, Terraria.GameContent.Events.DD2Event.TimeLeftBetweenWaves / 3 * 50), true, true);
			switch (Main.invasionProgressIcon == 3 ? Terraria.GameContent.Events.DD2Event.OngoingDifficulty : 0) {
				case 1:
					if (Main.invasionProgressWave > 5 || (Main.invasionProgressWave == 5 && Main.invasionProgress >= Main.invasionProgressMax - 1))
						return "Defeat the Dark Mage!";
					goto default;
				case 2:
					if (Main.invasionProgressWave > 7 || (Main.invasionProgressWave == 7 && Main.invasionProgress >= Main.invasionProgressMax - 1))
						return "Defeat the Ogre!";
					goto default;
				case 3:
					if (NPC.waveNumber >= 7)
						return "Defeat Betsy!";
					goto default;
				default:
					return $"{Main.invasionProgress * 100 / Main.invasionProgressMax}% ({Main.invasionProgressMax - Main.invasionProgress})";
			}
		}

		public override void PatchMethodBody(MethodDef method) {
			// Replace `text2 = (int)((float)invasionProgress * 100f / (float)invasionProgressMax) + "%"`
			// with `text2 = GetProgressString()`.
			var replacements = 0;
			var instructions = method.Body.Instructions;
			for (int i = 0; i < instructions.Count; i++) {
				if (instructions[i].Is(Code.Ldsfld) && ((IField) instructions[i].Operand).Name == nameof(Main.invasionProgress)
					&& instructions[i + 2].IsConstant(100f)) {
					instructions[i].OpCode = OpCodes.Nop;  // Nop it to preserve branches to it.
					instructions[i].Operand = null;
					instructions.Insert(i + 1, Call(GetProgressString));
					i += 2;
					while (!instructions[i].IsStloc())
						instructions.RemoveAt(i);
					++replacements;
				}
			}
			if (replacements == 0) throw new ArgumentException("Couldn't find code to replace.");
		}
	}
}
