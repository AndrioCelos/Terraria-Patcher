#nullable enable

using System;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

using Terraria;

namespace TerrariaPatcher.PatchSets;

internal class InfoAccessoryDisplayFix : PatchSet {
	public override string Name => "Info Accessory Display Fix";
	public override Version Version => new(1, 0);
	public override string Description => "Shifts the info accessory display according to the minimap scale.";

	internal class InfoAccessoryDisplayFixPatch : Patch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(Main), "GetInfoAccIconPosition");

		public override void PatchMethodBody(MethodDef method) {
			var replacements = 0;
			var instructions = method.Body.Instructions;
			for (int i = 0; i < instructions.Count; i++) {
				if (instructions[i].IsConstant(261)) {
					instructions.Insert(i + 1, OpCodes.Conv_R4.ToInstruction());
					instructions.Insert(i + 2, LoadField(typeof(Main), nameof(Main.MapScale)));
					instructions.Insert(i + 3, OpCodes.Mul.ToInstruction());
					instructions.Insert(i + 4, OpCodes.Conv_I4.ToInstruction());
					i += 4;
					replacements++;
				}
			}
			if (replacements == 0) throw new ArgumentException("Couldn't find code to replace.");
		}
	}
}
