#nullable enable

using System;
using System.Collections.Generic;

using dnlib.DotNet.Emit;
using dnlib.DotNet;

using ReLogic.Graphics;

using Terraria;
using Terraria.GameContent;
using Terraria.UI;

using TerrariaPatcher.Mods;
using Terraria.IO;
using System.Runtime.InteropServices;

namespace TerrariaPatcher.PatchSets;

internal class PauseOnLostFocusToggle : PatchSet {
	public override string Name => "Pause on Lost Focus Toggle";
	public override Version Version => new(1, 0);
	public override string Description => "Allows you to choose whether the game pauses when the window is not focused.";
	public override IReadOnlyCollection<Type> Dependencies => new[] { typeof(Commands) };

	public static bool PauseOnLostFocus;
	
	internal class InitializePatch : MainInitializePatch {
		public static void Prefix() {
			CommandManager.Commands.Add("pausefocus", new((_, _, args) => {
				PauseOnLostFocus = args[0].ToLower() switch {
					"on" or "true" => true,
					"toggle" => !PauseOnLostFocus,
					_ => false
				};
				CommandManager.SuccessMessage(PauseOnLostFocus
					? "Pause on lost focus is enabled."
					: "Pause on lost focus is disabled.");
			}, 1, 1, "on|off|toggle", "Chooses whether to pause the game when the window is not focused."));
		}
	}

	internal class SaveSettingsPatch : Patch {
		public override PatchTarget TargetMethod => PatchTarget.Create(Main.SaveSettings);

		public override void PatchMethodBody(MethodDef method) {
			var instructions = method.Body.Instructions;
			for (var i = 4; i < instructions.Count; i++) {
				if (instructions[i - 4].Is(Code.Ldsfld) && ((IField) instructions[i - 4].Operand).Name == nameof(Main.Configuration)
				&& instructions[i - 3].Is(Code.Ldc_I4_1)
				&& instructions[i - 2].Is(Code.Callvirt) && ((IMethod) instructions[i - 2].Operand).Name == nameof(Preferences.Save)
				&& instructions[i - 1].Is(Code.Brfalse_S)) {
					instructions.Insert(i - 4, this.Call(PutModSettings));
					return;
				}
			}
			throw new ArgumentException("Couldn't find code to replace.");
		}

		public static void PutModSettings() {
			Main.Configuration.Put(nameof(PauseOnLostFocus), PauseOnLostFocus);
		}
	}

	internal class LoadSettingsPatch : Patch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(Main), "LoadSettings");

		public override void PatchMethodBody(MethodDef method) {
			var instructions = method.Body.Instructions;
			for (var i = 3; i < instructions.Count; i++) {
				if (instructions[i - 3].Is(Code.Ldloc_1)
				&& instructions[i - 2].IsConstant(279)
				&& instructions[i - 1].Is(Code.Beq_S)) {
					instructions.Insert(i - 3, this.Call(GetModSettings));
					return;
				}
			}
			throw new ArgumentException("Couldn't find code to replace.");
		}

		public static void GetModSettings() {
			PauseOnLostFocus = Main.Configuration.Get<bool>(nameof(PauseOnLostFocus), true);
		}
	}

	internal class DoUpdatePatch : Patch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(Main), "DoUpdate");

		public override void PatchMethodBody(MethodDef method) {
			var instructions = method.Body.Instructions;
			// Find the check `if (!hasFocus && netMode == 0)`.
			for (var i = 1; i < instructions.Count; i++) {
				if (instructions[i - 1].Is(Code.Ldsfld) && ((IField) instructions[i - 1].Operand).Name == nameof(Main.hasFocus)
				&& instructions[i].Is(Code.Brtrue_S)) {
					var branchTarget = (Instruction) instructions[i].Operand;
					// Make `gamePaused = true; return;` conditional.
					for (; i < instructions.Count; i++) {
						if (instructions[i - 1].Is(Code.Ldc_I4_1)
						&& instructions[i].Is(Code.Stsfld)) {
							var oldInstruction = instructions[i - 1];
							var newInstruction = this.LoadField(typeof(PauseOnLostFocusToggle), nameof(PauseOnLostFocus));
							instructions.Insert(i - 1, newInstruction);
							instructions.Insert(i + 0, OpCodes.Brfalse_S.ToInstruction(branchTarget));

							// Repoint the branch that pointed at this instruction.
							for (; i >= 0; i--) {
								if (instructions[i].Operand == oldInstruction) {
									instructions[i].Operand = newInstruction;
									return;
								}
							}
							throw new ArgumentException("Couldn't find branch to repoint.");
						}
					}
				}
			}
			throw new ArgumentException("Couldn't find code to replace.");
		}
	}
}
