#nullable enable

using System;
using System.Collections.Generic;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

using Microsoft.Xna.Framework;

using Terraria;
using Terraria.Audio;
using Terraria.ID;

using TerrariaPatcher.Mods;

namespace TerrariaPatcher.PatchSets;

internal class TallyCounter : PatchSet {
	public override string Name => "Tally Counter";
	public override Version Version => new(1, 0);
	public override string Description => "Use the tally counter as an actual counter with a client-side command.";
	public override IReadOnlyCollection<Type> Dependencies => new[] { typeof(ColouredInfoAccessories), typeof(Commands) };

	internal static bool CounterShowing;
	internal static int CounterCount;

	internal static string GetTitle(string normalTitle) => CounterShowing ? Lang.GetItemName(ItemID.TallyCounter).Value : normalTitle;

	internal static string? GetDisplayString(ref Color infoColour) {
		if (CounterShowing) {
			infoColour.R = (byte) (infoColour.R * ModManager.AccentColor.R / 255);
			infoColour.G = (byte) (infoColour.G * ModManager.AccentColor.G / 255);
			infoColour.B = (byte) (infoColour.B * ModManager.AccentColor.B / 255);
			infoColour.A = (byte) (infoColour.A * ModManager.AccentColor.A / 255);
			return $"Count: {CounterCount}";
		} else
			return null;
	}

	internal class InitializePatch : MainInitializePatch {
		public static void Prefix() => CommandManager.Commands.Add("counter", CommandCounter);
	}

	public static void CommandCounter(string[] args) {
		var soundID = SoundID.MenuTick;
		switch (args[0].ToLower()) {
			case "hide": CounterShowing = false; break;
			case "show": CounterShowing = true; break;
			case "toggle": CounterShowing = !CounterShowing; break;
			case "-": case "+": case "=": case "set":
				int n;
				if (args.Length > 1) {
					if (!int.TryParse(args[1], out n)) {
						CommandManager.FailMessage($"Invalid number.");
						return;
					}
				} else if (args[0][0] is '-' or '+') {
					n = 1;
				} else {
					CommandManager.FailMessage($"This command requires a number.");
					return;
				}
				switch (args[0][0]) {
					case '-': CounterCount -= n; break;
					case '+': CounterCount += n; break;
					default:  CounterCount = n; soundID = SoundID.Unlock; break;
				}
				CounterShowing = true;
				break;
			case "reset":
				CounterShowing = true;
				CounterCount = 0;
				soundID = SoundID.Unlock;
				break;
			default:
				CommandManager.FailMessage($"Unknown stopwatch command: {args[0]}");
				return;
		}
		SoundEngine.PlaySound(soundID);
	}

	internal class DrawInfoAccsPatch : Patch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(Main), "DrawInfoAccs");

		public override void PatchMethodBody(MethodDef method) {
			// Replace the code after `text3 = Lang.inter[101].Value` that builds the display string.
			var instructions = method.Body.Instructions;
			for (int i = 3; i < instructions.Count; i++) {
				if (instructions[i - 3].IsConstant(101)
					&& instructions[i].IsStloc()) {
					i++;

					for (var j = i; ; j++) {
						if (instructions[j].Is(Code.Br) || instructions[j].Is(Code.Br_S)) {
							// Copy this jump if we want to override the display text.
							var jumpInstruction = instructions[j].Operand;
							var local = instructions[j - 1].GetLocal(method.Body.Variables);

							// Now add our code.
							instructions.Insert(i, OpCodes.Ldloca_S.ToInstruction(ColouredInfoAccessories.InfoColourLocal));
							instructions.Insert(i + 1, Call(TallyCounter.GetDisplayString));
							instructions.Insert(i + 2, OpCodes.Stloc_S.ToInstruction(local));
							instructions.Insert(i + 3, OpCodes.Ldloc_S.ToInstruction(local));
							instructions.Insert(i + 4, new(OpCodes.Brtrue, jumpInstruction));

							instructions.Insert(i - 1, Call(TallyCounter.GetTitle));

							return;
						}
					}
				}
			}
			throw new ArgumentException("Couldn't find code to replace.");
		}
	}
}
