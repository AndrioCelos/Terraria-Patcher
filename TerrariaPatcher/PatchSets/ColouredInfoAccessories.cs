#nullable enable

using System;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

using Microsoft.Xna.Framework;

using Terraria;

namespace TerrariaPatcher.PatchSets;

internal class ColouredInfoAccessories : PatchSet {
	public override string Name => "Coloured Info Accessories";
	public override Version Version => new(1, 0);
	public override string Description => "Allows other mods to change the colour of info accessory displays.";

	[NoCopyToTarget]
	private static Local? infoColourLocal;
	[NoCopyToTarget]
	public static Local InfoColourLocal {
		[NoCopyToTarget]
		get {
			// The Terraria assembly must be loaded here.
			if (infoColourLocal is not null) return infoColourLocal;
			infoColourLocal = new(Program.TargetModules[0].ModuleDef.ImportAsTypeSig(typeof(Color)), "infoColour");
			return infoColourLocal;
		}
	}

	internal class DrawInfoAccsPatch : Patch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(Main), "DrawInfoAccs");

		public override void PatchMethodBody(MethodDef method) {
			// Replace `color2 = new Color(mouseTextColor, mouseTextColor, mouseTextColor, mouseTextColor)` with `colour2 = infoColour`
			// and give the existing initialiser to infoColour before `text2 = ""`.
			var infoColourLocal = InfoColourLocal;
			method.Body.Variables.Add(infoColourLocal);

			int pos;
			for (pos = 0; pos < method.Body.Instructions.Count; pos++) {
				if (method.Body.Instructions[pos].IsBr()) break;
			}
			for (pos++; pos < method.Body.Instructions.Count; pos++) {
				if (method.Body.Instructions[pos].IsConstant("")) {
					pos += 4;  // The initialiser will be inserted after the two empty string ones.
					break;
				}
			}

			var instructions = method.Body.Instructions;
			for (int i = 5; i < instructions.Count; i++) {
				if (instructions[i - 1].Is(Code.Ldsfld) && ((IField) instructions[i - 1].Operand).Name == nameof(Main.mouseTextColor)
					&& instructions[i].Is(Code.Call)) {
					var callInstruction = instructions[i];
					var mouseTextColorInstruction4 = instructions[i - 1];
					var mouseTextColorInstruction3 = instructions[i - 2];
					var mouseTextColorInstruction2 = instructions[i - 3];
					var mouseTextColorInstruction1 = instructions[i - 4];
					var local = (Local) instructions[i - 5].Operand;
					instructions[i - 5] = OpCodes.Ldloc.ToInstruction(infoColourLocal);
					instructions[i - 4] = OpCodes.Stloc.ToInstruction(local);
					instructions.RemoveAt(i);
					instructions.RemoveAt(i - 1);
					instructions.RemoveAt(i - 2);
					instructions.RemoveAt(i - 3);

					method.Body.Instructions.Insert(pos, OpCodes.Ldloca_S.ToInstruction(infoColourLocal));
					method.Body.Instructions.Insert(pos + 1, mouseTextColorInstruction1);
					method.Body.Instructions.Insert(pos + 2, mouseTextColorInstruction2);
					method.Body.Instructions.Insert(pos + 3, mouseTextColorInstruction3);
					method.Body.Instructions.Insert(pos + 4, mouseTextColorInstruction4);
					method.Body.Instructions.Insert(pos + 5, callInstruction);

					return;
				}
			}
			throw new ArgumentException("Couldn't find code to replace.");
		}
	}
}
