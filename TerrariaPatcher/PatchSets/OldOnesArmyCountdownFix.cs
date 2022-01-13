#nullable enable

using System;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

using Terraria;

namespace TerrariaPatcher.PatchSets;

internal class OldOnesArmyCountdownFix : PatchSet {
	public override string Name => "Old One's Army Countdown Fix";
	public override Version Version => new(1, 0);
	public override string Description => "Changes the intermission countdown during the Old One's Army event to round up instead of down.";

	internal class OldOnesArmyCountdownPatch : Patch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(Main), "DrawInterface_14_EntityHealthBars");

		public override void PatchMethodBody(MethodDef method) {
			// Replace `DD2Event.TimeLeftBetweenWaves / 60` with `(DD2Event.TimeLeftBetweenWaves + 59) / 60`.
			var instructions = method.Body.Instructions;
			for (int i = 1; i < instructions.Count; i++) {
				if (instructions[i].Is(Code.Div) && instructions[i - 1].IsConstant(60)) {
					instructions.Insert(i - 1, OpCodes.Ldc_I4_S.ToInstruction((sbyte) 59));
					instructions.Insert(i, OpCodes.Add.ToInstruction());
					return;
				}
			}
			throw new ArgumentException("Couldn't find code to replace.");
		}
	}
}
