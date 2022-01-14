#nullable enable

using System;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

using Terraria;
using Terraria.ID;

namespace TerrariaPatcher.PatchSets;

internal class DungeonGuardianBossMusic : PatchSet {
	public override string Name => "Dungeon Guardian Boss Music";
	public override Version Version => new(1, 0);
	public override string Description => "Gives Dungeon Guardians boss music.";

	internal class DungeonGuardianBossMusicPatch : Patch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(Main), "UpdateAudio_DecideOnNewMusic");

		public override void PatchMethodBody(MethodDef method) {
			// Adds the Dungeon Guardian after the Eater of Worlds part of the switch statement giving them boss music.
			var instructions = method.Body.Instructions;
			for (int i = 4; i < instructions.Count; i++) {
				if (instructions[i - 4].Is(Code.Ldloc_S)
					&& instructions[i - 3].IsConstant(NPCID.EaterofWorldsHead)
					&& instructions[i - 2].Is(Code.Sub)
					&& instructions[i - 1].Is(Code.Ldc_I4_2)
					&& (instructions[i].Is(Code.Ble_Un) || instructions[i].Is(Code.Ble_Un_S))) {
					instructions.Insert(i + 1, new(OpCodes.Ldloc_S, instructions[i - 4].Operand));
					instructions.Insert(i + 2, new(OpCodes.Ldc_I4_S, (sbyte) NPCID.DungeonGuardian));
					instructions.Insert(i + 3, new(OpCodes.Beq, instructions[i].Operand));
					return;
				}
			}
			throw new ArgumentException("Couldn't find where to insert code.");
		}
	}
}
