#nullable enable

using System;
using System.Collections.Generic;
using System.IO;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

using Microsoft.Xna.Framework;

using Newtonsoft.Json;

using Terraria;
using Terraria.Audio;
using Terraria.ID;

using TerrariaPatcher.Mods;

namespace TerrariaPatcher.PatchSets;

internal class StopwatchAndTallyCounter : PatchSet {

	public class StopwatchState {
		public bool Showing { get; set; }
		public bool Running { get; set; }
		public bool CountDown { get; set; }
		public long Time { get; set; }
		public long SplitTime { get; set; }
		public bool IsEmpty => !this.Showing && !this.Running && !this.CountDown && this.Time == 0 && this.SplitTime == 0;
	}

	public class CounterState {
		public bool Showing { get; set; }
		public int Count { get; set; }
		public bool IsEmpty => !this.Showing && this.Count == 0;
	}

	public class SaveData {
		public CounterState Counter { get; set; } = new();
		public StopwatchState Stopwatch { get; set; } = new();
	}

	public override string Name => "Stopwatch and Tally Counter";
	public override Version Version => new(2, 0);
	public override string Description => "Use the stopwatch and tally counter as an actual stopwatch and tally counter with client commands.";
	public override IReadOnlyCollection<Type> Dependencies => new[] { typeof(ColouredInfoAccessories), typeof(Commands) };

	public static CounterState Counter = new();
	public static StopwatchState Stopwatch = new();
	internal static Dictionary<string, SaveData> saveData = new();
	internal static string? lastCharacterName;

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
			bool minus = false;
			var time = Stopwatch.SplitTime != 0 ? Stopwatch.SplitTime : Stopwatch.Time;
			if (time < 0) {
				minus = true;
				time = -time;
			}

			var minutes = time / 3600;
			var seconds = time / 60 % 60;
			var cs = time % 60 * 5 / 3;

			infoColour.R = (byte) (infoColour.R * ModManager.AccentColor.R / 255);
			infoColour.G = (byte) (infoColour.G * ModManager.AccentColor.G / 255);
			infoColour.B = (byte) (infoColour.B * ModManager.AccentColor.B / 255);
			infoColour.A = (byte) (infoColour.A * ModManager.AccentColor.A / 255);
			
			return $"{(minus ? "-" : "")}{minutes:00}' {seconds:00}.{cs:00}\"";
		} else
			return null;
	}

	internal class InitializePatch : MainInitializePatch {
		public static void Prefix() {
			CommandManager.Commands.Add("counter", new(CommandCounter, 1, 2, "hide/show/toggle/- [number]/+ [number]/set <number>",
				   "Shows, hides or controls the mod tally counter."));
			CommandManager.Commands.Add("stopwatch", new(CommandStopwatch, 1, 2, "hide/show/toggle/start/stop/startstop/reset/restart/split/set <mins>/set <mins>:<secs>",
				   "Shows, hides or controls the mod stopwatch."));
			Player.Hooks.OnEnterWorld += Hooks_OnEnterWorld;
			Main.OnTickForInternalCodeOnly += Main_OnTick;
			// This event must be used because OnTickForThirdPartySoftwareOnly may be called many times between frames, or while paused.

			if (File.Exists(Path.Combine(Main.SavePath, "accessoryData.json"))) {
				using var reader = new JsonTextReader(new StreamReader(Path.Combine(Main.SavePath, "accessoryData.json")));
				saveData = new JsonSerializer().Deserialize<Dictionary<string, SaveData>>(reader);
			}
		}
	}

	private static void Hooks_OnEnterWorld(Player _) {
		if (Main.player[Main.myPlayer]?.name != lastCharacterName) {
			lastCharacterName = Main.player[Main.myPlayer]?.name;
			if (lastCharacterName is not null) {
				saveData.TryGetValue(lastCharacterName, out var entry);
				Counter = entry?.Counter ?? new();
				Stopwatch = entry?.Stopwatch ?? new();
			}
		}
	}

	private static void Main_OnTick() {
		if (Stopwatch.Running) {
			if (Stopwatch.CountDown) Stopwatch.Time--;
			else Stopwatch.Time++;
		}
	}

	public static void CommandCounter(Command command, string label, string[] args) {
		var soundID = SoundID.MenuTick;
		switch (args[0].ToLower()) {
			case "hide": Counter.Showing = false; break;
			case "show": Counter.Showing = true; break;
			case "toggle": Counter.Showing = !Counter.Showing; break;
			case "-": case "+": case "=": case "set":
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
				Stopwatch.CountDown = false;
				Stopwatch.Time = 0;
				Stopwatch.SplitTime = 0;
				break;
			case "restart":
				Stopwatch.Showing = true;
				Stopwatch.Running = true;
				Stopwatch.CountDown = false;
				Stopwatch.Time = 0;
				Stopwatch.SplitTime = 0;
				break;
			case "split":
				Stopwatch.Showing = true;
				Stopwatch.SplitTime = Stopwatch.SplitTime != 0 ? 0 : Stopwatch.Time;
				break;
			case "set":
				if (args.Length <= 1) goto default;

				var tokens = args[1].Split(new[] { ' ', ':', '\'', '"', 'm', 's' }, 2, StringSplitOptions.RemoveEmptyEntries);
				if (tokens.Length > 1) {
					if (double.TryParse(tokens[0], out var mins) && double.TryParse(tokens[1], out var secs)) {
						Stopwatch.Time = (int) Math.Round(mins * 3600 + secs * 60);
					} else {
						CommandManager.FailMessage("Invalid numbers.");
						return;
					}
				} else {
					if (double.TryParse(tokens[0], out var mins)) {
						Stopwatch.Time = (int) Math.Round(mins * 3600);
					} else {
						CommandManager.FailMessage("Invalid numbers.");
						return;
					}
				}

				Stopwatch.Showing = true;
				Stopwatch.Running = false;
				Stopwatch.CountDown = true;
				Stopwatch.SplitTime = 0;
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
							i = j + 6;
							break;
						}
					}
				} else if (instructions[i - 3].IsConstant(103)
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

							i = j + 6;
							stopwatchReplaced = true;
							break;
						}
					}
				}
			}
			if (!counterReplaced || !stopwatchReplaced) throw new ArgumentException("Couldn't find code to replace.");
		}
	}

	internal class SavePlayerPatch : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(Player.SavePlayer);

		public static void Prefix() {
			if (lastCharacterName is not null) {
				if (Counter.IsEmpty && Stopwatch.IsEmpty)
					saveData.Remove(lastCharacterName);
				else
					saveData[lastCharacterName] = new() { Counter = Counter, Stopwatch = Stopwatch };
				var file = Path.Combine(Main.SavePath, "accessoryData.json");
				if (saveData.Count == 0) {
					if (File.Exists(file))
						File.Delete(file);
				} else {
					using var writer = new JsonTextWriter(new StreamWriter(file));
					new JsonSerializer().Serialize(writer, saveData);
				}
			}
		}
	}
}
