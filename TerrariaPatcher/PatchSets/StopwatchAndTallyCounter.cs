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

internal class StopwatchAndTallyCounter : PatchSet {
	public class StopwatchState {
		public bool Showing { get; set; }
		public bool Running { get; set; }
		public long Time { get; set; }
		public long SplitTime { get; set; }
	}

	public class CounterState {
		public bool Showing { get; set; }
		public int Count { get; set; }
	}

	public override string Name => "Stopwatch and Tally Counter";
	public override Version Version => new(2, 0);
	public override string Description => "Use the stopwatch and tally counter as an actual stopwatch and tally counter with client commands.";
	public override IReadOnlyCollection<Type> Dependencies => new[] { typeof(ColouredInfoAccessories), typeof(Commands) };

	public static CounterState Counter = new();
	public static StopwatchState Stopwatch = new();
	public static int lastWorldID;

	internal static string GetCounterTitle(string normalTitle) => Counter.Showing ? Lang.GetItemName(ItemID.TallyCounter).Value : normalTitle;
	internal static string GetStopwatchTitle(string normalTitle) => Stopwatch.Showing ? Lang.GetItemName(ItemID.Stopwatch).Value : normalTitle;

	internal static string? GetCounterDisplayString(ref Color infoColour) {
		if (Counter.Showing) {
			infoColour.R = (byte) (infoColour.R * ModManager.AccentColor.R / 255);
			infoColour.G = (byte) (infoColour.G * ModManager.AccentColor.G / 255);
			infoColour.B = (byte) (infoColour.B * ModManager.AccentColor.B / 255);
			infoColour.A = (byte) (infoColour.A * ModManager.AccentColor.A / 255);
			return $"Count: {Counter.Count}";
		} else
			return null;
	}

	internal static string? GetStopwatchDisplayString(ref Color infoColour) {
		if (Stopwatch.Showing) {
			var time = Stopwatch.SplitTime != 0 ? Stopwatch.SplitTime : Stopwatch.Time;
			var minutes = time / 3600;
			var seconds = time / 60 % 60;
			var cs = time % 60 * 5 / 3;

			infoColour.R = (byte) (infoColour.R * ModManager.AccentColor.R / 255);
			infoColour.G = (byte) (infoColour.G * ModManager.AccentColor.G / 255);
			infoColour.B = (byte) (infoColour.B * ModManager.AccentColor.B / 255);
			infoColour.A = (byte) (infoColour.A * ModManager.AccentColor.A / 255);
			
			return $"{minutes:00}' {seconds:00}.{cs:00}\"";
		} else
			return null;
	}

	internal class InitializePatch : MainInitializePatch {
		public static void Prefix() {
			CommandManager.Commands.Add("counter", new(CommandCounter, 1, 2, "hide/show/toggle/- [number]/+ [number]/set <number>",
				   "Shows, hides or controls the mod tally counter."));
			CommandManager.Commands.Add("stopwatch", new(CommandStopwatch, 1, 1, "hide/show/toggle/start/stop/startstop/reset/restart/split",
				   "Shows, hides or controls the mod stopwatch."));
			Player.Hooks.OnEnterWorld += Hooks_OnEnterWorld;
			Main.OnTickForThirdPartySoftwareOnly += Main_OnTickForThirdPartySoftwareOnly;
		}
	}

	private static void Hooks_OnEnterWorld(Player _) {
		// Currently the stopwatch is not saved if you leave and enter a different world.
		if (Main.worldID != lastWorldID) {
			lastWorldID = Main.worldID;
			Stopwatch.Showing = false;
			Stopwatch.Running = false;
			Stopwatch.Time = 0;
			Stopwatch.SplitTime = 0;
		}
	}

	private static void Main_OnTickForThirdPartySoftwareOnly() {
		if (Stopwatch.Running) Stopwatch.Time++;
	}

	public static void CommandCounter(Command command, string label, string[] args) {
		var soundID = SoundID.MenuTick;
		switch (args[0].ToLower()) {
			case "hide": Counter.Showing = false; break;
			case "show": Counter.Showing = true; break;
			case "toggle": Counter.Showing = !Counter.Showing; break;
			case "-":
			case "+":
			case "=":
			case "set":
				int n;
				if (args.Length > 1) {
					if (!int.TryParse(args[1], out n)) {
						CommandManager.FailMessage("Invalid number.");
						return;
					}
				} else if (args[0][0] is '-' or '+') {
					n = 1;
				} else {
					command.ShowParametersFailMessage(label);
					return;
				}
				switch (args[0][0]) {
					case '-': Counter.Count -= n; break;
					case '+': Counter.Count += n; break;
					default: Counter.Count = n; soundID = SoundID.Unlock; break;
				}
				Counter.Showing = true;
				break;
			case "reset":
				Counter.Showing = true;
				Counter.Count = 0;
				soundID = SoundID.Unlock;
				break;
			default:
				command.ShowParametersFailMessage(label);
				return;
		}
		SoundEngine.PlaySound(soundID);
	}

	public static void CommandStopwatch(Command command, string label, string[] args) {
		switch (args[0].ToLower()) {
			case "hide": Stopwatch.Showing = false; break;
			case "show": Stopwatch.Showing = true; break;
			case "toggle": Stopwatch.Showing = !Stopwatch.Showing; break;
			case "startstop":
				Stopwatch.Showing = true;
				Stopwatch.Running = !Stopwatch.Running;
				break;
			case "stop":
				Stopwatch.Showing = true;
				Stopwatch.Running = false;
				break;
			case "start":
				Stopwatch.Showing = true;
				Stopwatch.Running = true;
				break;
			case "reset":
				Stopwatch.Showing = true;
				Stopwatch.Running = false;
				Stopwatch.Time = 0;
				Stopwatch.SplitTime = 0;
				break;
			case "restart":
				Stopwatch.Showing = true;
				Stopwatch.Running = true;
				Stopwatch.Time = 0;
				Stopwatch.SplitTime = 0;
				break;
			case "split":
				Stopwatch.Showing = true;
				Stopwatch.SplitTime = Stopwatch.SplitTime != 0 ? 0 : Stopwatch.Time;
				break;
			default:
				command.ShowParametersFailMessage(label);
				return;
		}
		SoundEngine.PlaySound(SoundID.MenuTick);
	}

	internal class DrawInfoAccsPatch : Patch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(Main), "DrawInfoAccs");

		public override void PatchMethodBody(MethodDef method) {
			// Replace the code after `text3 = Lang.inter[N].Value` that builds the display string.
			bool counterReplaced = false, stopwatchReplaced = false;
			var instructions = method.Body.Instructions;
			for (int i = 3; i < instructions.Count && !(counterReplaced && stopwatchReplaced); i++) {
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
							instructions.Insert(i + 1, Call(StopwatchAndTallyCounter.GetCounterDisplayString));
							instructions.Insert(i + 2, OpCodes.Stloc_S.ToInstruction(local));
							instructions.Insert(i + 3, OpCodes.Ldloc_S.ToInstruction(local));
							instructions.Insert(i + 4, new(OpCodes.Brtrue, jumpInstruction));

							instructions.Insert(i - 1, Call(StopwatchAndTallyCounter.GetCounterTitle));
							counterReplaced = true;
						}
					}
				}
				if (instructions[i - 3].IsConstant(103)
					&& instructions[i].IsStloc()) {
					i++;

					var foundBaseText = false;
					for (var j = i; ; j++) {
						if (!foundBaseText) {
							if (instructions[j].IsConstant("GameUI.Speed"))
								foundBaseText = true;
						} else if (instructions[j].IsStloc()) {
							// Jump after here if we want to override the stopwatch text.
							var jumpInstruction = instructions[j + 1];
							var local = instructions[j].GetLocal(method.Body.Variables);

							// Now add our code.
							instructions.Insert(i, OpCodes.Ldloca_S.ToInstruction(ColouredInfoAccessories.InfoColourLocal));
							instructions.Insert(i + 1, Call(StopwatchAndTallyCounter.GetStopwatchDisplayString));
							instructions.Insert(i + 2, OpCodes.Stloc_S.ToInstruction(local));
							instructions.Insert(i + 3, OpCodes.Ldloc_S.ToInstruction(local));
							instructions.Insert(i + 4, new(OpCodes.Brtrue, jumpInstruction));
							
							instructions.Insert(i - 1, Call(StopwatchAndTallyCounter.GetStopwatchTitle));
							stopwatchReplaced = true;
						}
					}
				}
			}
			if (!counterReplaced || !stopwatchReplaced) throw new ArgumentException("Couldn't find code to replace.");
		}
	}
}
