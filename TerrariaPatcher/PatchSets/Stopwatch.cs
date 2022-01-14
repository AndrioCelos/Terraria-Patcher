﻿#nullable enable

using System;
using System.Collections.Generic;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

using Terraria;
using Terraria.Audio;
using Terraria.ID;

using TerrariaPatcher.Mods;

namespace TerrariaPatcher.PatchSets;

internal class Stopwatch : PatchSet {
	public override string Name => "Stopwatch";
	public override Version Version => new(1, 0);
	public override string Description => "Use the stopwatch as an actual stopwatch with a client-side command.";
	public override IReadOnlyCollection<Type> Dependencies => new[] { typeof(Commands) };

	internal static bool StopwatchShowing;
	internal static bool StopwatchRunning;
	internal static int StopwatchTime;
	internal static int StopwatchSplitTime;
	internal static int lastWorldID;

	internal static string? GetDisplayString() {
		if (StopwatchShowing) {
			var time = StopwatchSplitTime != 0 ? StopwatchSplitTime : StopwatchTime;
			var minutes = time / 3600;
			var seconds = time / 60 % 60;
			var cs = time % 60 * 5 / 3;
			return $"{minutes:00}' {seconds:00}.{cs:00}\"";
		}
		return null;
	}

	internal class InitializePatch : MainInitializePatch {
		public static void Prefix() {
			CommandManager.Commands.Add("stopwatch", CommandStopwatch);
			Player.Hooks.OnEnterWorld += Hooks_OnEnterWorld;
			Main.OnTickForInternalCodeOnly += Main_OnTickForInternalCodeOnly;
		}
	}

	private static void Hooks_OnEnterWorld(Player obj) {
		// Currently the stopwatch is not saved if you leave and enter a different world.
		if (Main.worldID != lastWorldID) {
			lastWorldID = Main.worldID;
			StopwatchShowing = false;
			StopwatchRunning = false;
			StopwatchTime = 0;
			StopwatchSplitTime = 0;
		}
	}

	private static void Main_OnTickForInternalCodeOnly() {
		if (StopwatchRunning) StopwatchTime++;
	}
	
	public static void CommandStopwatch(string[] args) {
		switch (args[0].ToLower()) {
			case "hide": StopwatchShowing = false; break;
			case "show": StopwatchShowing = true; break;
			case "toggle": StopwatchShowing = !StopwatchShowing; break;
			case "startstop":
				StopwatchShowing = true;
				StopwatchRunning = !StopwatchRunning;
				break;
			case "stop":
				StopwatchShowing = true;
				StopwatchRunning = false;
				break;
			case "start":
				StopwatchShowing = true;
				StopwatchRunning = true;
				break;
			case "reset":
				StopwatchShowing = true;
				StopwatchRunning = false;
				StopwatchTime = 0;
				StopwatchSplitTime = 0;
				break;
			case "restart":
				StopwatchShowing = true;
				StopwatchRunning = true;
				StopwatchTime = 0;
				StopwatchSplitTime = 0;
				break;
			case "split":
				StopwatchShowing = true;
				StopwatchSplitTime = StopwatchSplitTime != 0 ? 0 : StopwatchTime;
				break;
			default:
				CommandManager.FailMessage($"Unknown stopwatch command: {args[0]}");
				return;
		}
		SoundEngine.PlaySound(SoundID.MenuTick);
	}

	internal class DrawInfoAccsPatch : Patch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(Main), "DrawInfoAccs");

		public override void PatchMethodBody(MethodDef method) {
			// Replace the code after `text3 = Lang.inter[103].Value` that builds the display string.
			var instructions = method.Body.Instructions;
			for (int i = 3; i < instructions.Count; i++) {
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
							instructions.Insert(i    , Call(Stopwatch.GetDisplayString));
							instructions.Insert(i + 1, OpCodes.Stloc_S.ToInstruction(local));
							instructions.Insert(i + 2, OpCodes.Ldloc_S.ToInstruction(local));
							instructions.Insert(i + 3, OpCodes.Brtrue.ToInstruction(jumpInstruction));

							return;
						}
					}
				}
			}
			throw new ArgumentException("Couldn't find code to replace.");
		}
	}
}