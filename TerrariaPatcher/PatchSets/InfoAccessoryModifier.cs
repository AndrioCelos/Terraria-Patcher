#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace TerrariaPatcher.PatchSets;

internal class InfoAccessoryModifier : PatchSet {
	public override string Name => "Info Accessory Modifier";
	public override Version Version => new(1, 0);
	public override string Description => "Allows the info accessory displays to be modified.";

	public delegate void DrawInfoAccessoryHandler(int item, ref string title, ref string? text, ref Color colour, ref Color shadowColour);

	public static event DrawInfoAccessoryHandler? DrawInfoAccessory;

	[NoCopyToTarget]
	private static Local? infoColourLocal;
	[NoCopyToTarget]
	public static Local InfoColourLocal {
		[NoCopyToTarget]
		get {
			// The Terraria assembly must be loaded here.
			if (infoColourLocal is not null) return infoColourLocal;

			var method = PatchTarget.Create(typeof(Main), "DrawInfoAccs").GetMethodDefs().Single();
			var instructions = method.Body.Instructions;
			for (int i = 5; i < instructions.Count; i++) {
				if (instructions[i - 4].Is(Code.Ldsfld) && ((IField) instructions[i - 4].Operand).Name == nameof(Main.mouseTextColor)
					&& instructions[i - 3].Is(Code.Ldsfld) && ((IField) instructions[i - 3].Operand).Name == nameof(Main.mouseTextColor)
					&& instructions[i - 2].Is(Code.Ldsfld) && ((IField) instructions[i - 2].Operand).Name == nameof(Main.mouseTextColor)
					&& instructions[i - 1].Is(Code.Ldsfld) && ((IField) instructions[i - 1].Operand).Name == nameof(Main.mouseTextColor)
					&& instructions[i].Is(Code.Call)) {
					if (instructions[i - 5].Operand is Local local) {
						infoColourLocal = local;
						return local;
					}
				}
			}
			throw new InvalidOperationException("Can't find infoTextColor local.");
		}
	}

	[NoCopyToTarget]
	private static Local? infoShadowColourLocal;
	[NoCopyToTarget]
	public static Local InfoShadowColourLocal {
		[NoCopyToTarget]
		get {
			// The Terraria assembly must be loaded here.
			if (infoShadowColourLocal is not null) return infoShadowColourLocal;

			var method = PatchTarget.Create(typeof(Main), "DrawInfoAccs").GetMethodDefs().Single();
			var instructions = method.Body.Instructions;
			for (int i = 1; i < instructions.Count; i++) {
				if (instructions[i - 1].Is(Code.Call) && ((IMethod) instructions[i - 1].Operand).Name == "get_Black"
					&& instructions[i].Is(Code.Stloc_S)) {
					if (instructions[i].Operand is Local local) {
						infoShadowColourLocal = local;
						return local;
					}
				}
			}
			throw new InvalidOperationException("Can't find infoTextColor local.");
		}
	}

	internal class DrawInfoAccsPatch : Patch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(Main), "DrawInfoAccs");

		private static readonly Dictionary<int, int> LabelIndexMap = new() {
			{ 95, ItemID.GoldWatch },
			{ 96, ItemID.WeatherRadio },
			{ 102, ItemID.Sextant },
			{ 97, ItemID.FishFinder },
			{ 104, ItemID.MetalDetector },
			{ 105, ItemID.LifeformAnalyzer },
			{ 100, ItemID.Radar },
			{ 101, ItemID.TallyCounter },
			{ 106, ItemID.DPSMeter },
			{ 103, ItemID.Stopwatch },
			{ 98, ItemID.Compass },
			{ 99, ItemID.DepthMeter }
		};

		internal static string? OverrideDrawAccessory(int labelIndex, ref string title, ref Color colour, ref Color shadowColour) {
			if (!LabelIndexMap.TryGetValue(labelIndex, out var item)) return null;
			string? text = null;
			DrawInfoAccessory?.Invoke(item, ref title, ref text, ref colour, ref shadowColour);
			return text;
		}

		public override void PatchMethodBody(MethodDef method) {
			var handledLocal = method.Body.Variables.Add(new(method.Module.CorLibTypes.Boolean, "handled", method.Body.Variables.Count));

			// Replace the code after `text3 = Lang.inter[N].Value` that builds the display string.
			bool counterReplaced = false, stopwatchReplaced = false;
			var instructions = method.Body.Instructions;
			Instruction? endAccessoryIfChainInstruction = null;
			Local? infoTextLocal = null;
			Local? infoTitleLocal = null;

			// Now modify the accessories if chain.
			for (int i = 4; i < instructions.Count && !(counterReplaced && stopwatchReplaced); i++) {
				if (instructions[i - 3].Is(Code.Ldc_I4_S) && ((sbyte) instructions[i - 3].Operand) is >= 95 and <= 106
					&& instructions[i - 4].Is(Code.Ldsfld) && ((IField) instructions[i - 4].Operand).Name == nameof(Lang.inter)
					&& instructions[i].IsStloc()) {
					var accessoryLabelIndex = (sbyte) instructions[i - 3].Operand;
					infoTitleLocal = (Local) instructions[i].Operand;

					// Find the flag that indicates which displays have been drawn.
					Local? flagLocal = null;
					for (var j = i; j >= 0; j--) {
						if (instructions[j - 1].IsLdloc()
						&& instructions[j].Is(Code.Brtrue)) {
							flagLocal = instructions[j - 1].GetLocal(method.Body.Variables);
							break;
						}
					}
					if (flagLocal is null) throw new InvalidOperationException("Can't find the flag local");

					// Find the instruction that sets the flag.
					Instruction? targetInstruction = null;
					for (var j = i; j < method.Body.Instructions.Count; j++) {
						if (instructions[j - 1].Is(Code.Ldc_I4_1) && instructions[j].IsStloc() && instructions[j].GetLocal(method.Body.Variables) == flagLocal) {
							targetInstruction = instructions[j - 1];
							break;
						}
					}
					targetInstruction ??= targetInstruction;  // Some branches set the flag first, so we skip the whole branch in that case.

					i++;
					if (infoTextLocal is null || infoTitleLocal is null || endAccessoryIfChainInstruction is null) {
						if (accessoryLabelIndex != 95) throw new InvalidOperationException("First accessory in the code isn't the watch?!");
						// Find where to skip to after modifying the accessory display.
						for (var j = i; ; j++) {
							if (instructions[j].Is(Code.Br)) {
								endAccessoryIfChainInstruction = (Instruction) instructions[j].Operand;
								break;
							}
						}
						// Find local variables we need to modify.
						var foundColon = false;
						for (var j = i; ; j++) {
							if (!foundColon) {
								if (instructions[j].IsConstant(":"))
									foundColon = true;
							} else if (instructions[j].IsStloc()) {
								infoTextLocal = (Local) instructions[j].Operand;
								break;
							}
						}
					}

					// Now add our code.
					var originalInstruction = instructions[i];
					instructions.Insert(i + 0, OpCodes.Ldc_I4_S.ToInstruction(accessoryLabelIndex));
					instructions.Insert(i + 1, OpCodes.Ldloca_S.ToInstruction(infoTitleLocal));
					instructions.Insert(i + 2, OpCodes.Ldloca_S.ToInstruction(InfoColourLocal));
					instructions.Insert(i + 3, OpCodes.Ldloca_S.ToInstruction(InfoShadowColourLocal));
					instructions.Insert(i + 4, this.Call(OverrideDrawAccessory));
					instructions.Insert(i + 5, OpCodes.Stloc_S.ToInstruction(infoTextLocal));
					instructions.Insert(i + 6, OpCodes.Ldloc_S.ToInstruction(infoTextLocal));
					instructions.Insert(i + 7, new(OpCodes.Brfalse, originalInstruction));
					instructions.Insert(i + 8, new(OpCodes.Ldc_I4_1));
					instructions.Insert(i + 9, new(OpCodes.Stloc_S, flagLocal));
					instructions.Insert(i + 10, new(OpCodes.Br, endAccessoryIfChainInstruction));
				}
			}
		}
	}
}
