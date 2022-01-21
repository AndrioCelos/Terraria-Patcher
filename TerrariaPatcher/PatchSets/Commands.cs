#nullable enable

using System;
using System.Collections.Generic;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

using Microsoft.Xna.Framework.Input;

using Terraria;
using Terraria.GameInput;

using TerrariaPatcher.Mods;

namespace TerrariaPatcher.PatchSets;

internal class Commands : PatchSet {
	public override string Name => "Client Commands";
	public override Version Version => new(1, 1);
	public override string Description => "Adds client commands, used with the . prefix in chat or custom key bindings. See the readme file for more information.";
	public override IReadOnlyCollection<Type>? Dependencies => new[] { typeof(ModManagerMod), typeof(WordWrapFix) };

	public override void BeforeApply() {
		var keystrokeJsonConverterTypeDef = ImportType(typeof(KeystrokeJsonConverter), "Terraria");
		var keyBindingTypeDef = ImportType(typeof(KeyBinding), "Terraria");
		foreach (var p in keyBindingTypeDef.Properties) {
			foreach (var attr in p.CustomAttributes) {
				if (attr.ConstructorArguments.Count != 0 && attr.ConstructorArguments[0].Value is ClassSig classSig && classSig.TypeName == nameof(Mods.KeystrokeJsonConverter)) {
					attr.ConstructorArguments[0] = new(attr.ConstructorArguments[0].Type, new ClassSig(keystrokeJsonConverterTypeDef));
				}
			}
		}
		ImportType(typeof(Command), "Terraria");
		ImportType(typeof(CommandManager), "Terraria");
		ImportType(typeof(Keystroke), "Terraria");
		CopyFileToOutputDirectory("Microsoft.Bcl.HashCode.dll");
	}

	internal class InitializePatch : MainInitializePatch {
		public static void Prefix() => CommandManager.Initialise();
		public static void Postfix() => CommandManager.InitialisePost();
	}

	internal class DoUpdateEnterToggleChatPatch : Patch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(Main), "DoUpdate_Enter_ToggleChat");

		public static bool IsChatKeyPressed(KeyboardState keyState)
			=> keyState.IsKeyDown(Keys.Enter) || keyState.IsKeyDown(Keys.OemQuestion) || keyState.IsKeyDown(Keys.OemPeriod);

		public static string GetInitialChatString(KeyboardState keyState)
			=> keyState.IsKeyDown(Keys.OemQuestion) ? "/" : keyState.IsKeyDown(Keys.OemPeriod) ? "." : "";

		public override void PatchMethodBody(MethodDef method) {
			// Replace `keyState.IsKeyDown(Keys.Enter)` with `IsChatKeyPressed(keyState)`.
			bool replacedEnter = false;
			var instructions = method.Body.Instructions;
			for (int i = 0; i < instructions.Count; i++) {
				if (!replacedEnter) {
					if (instructions[i].Is(Code.Ldsflda)) {
						instructions[i].OpCode = OpCodes.Ldsfld;
					} else if (instructions[i].IsConstant((int) Keys.Enter)) {
						instructions[i] = this.Call(IsChatKeyPressed);
						replacedEnter = true;
						instructions.RemoveAt(i + 1);
					}
				} else if (instructions[i].IsConstant("")) {
					instructions[i] = this.LoadField(typeof(Main), nameof(Main.keyState));
					instructions.Insert(i + 1, this.Call(GetInitialChatString));
					i++;
				}
			}
			if (!replacedEnter) throw new ArgumentException("Couldn't find code to replace.");
		}
	}

	internal class DoUpdateHandleChatPatch : Patch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(Main), "DoUpdate_HandleChat");

		public static bool HandlePatchChat() {
			if (Main.chatText.StartsWith("./"))
				Main.chatText = $"[c:/]{Main.chatText.Substring(2)}";
			else if (Main.chatText[0] == '.' && !Main.chatText.StartsWith("..")) {
				CommandManager.HandleCommand(Main.chatText.Substring(1));
				return true;
			}
			return false;
		}

		public override void PatchMethodBody(MethodDef method) {
			// Replace `if (chatText != "")` with `if (chatText != "" && !HandlePatchChat())`.
			var instructions = method.Body.Instructions;
			for (int i = 2; i < instructions.Count; i++) {
				if (instructions[i - 2].IsConstant("")
					&& instructions[i - 1].Is(Code.Call) && ((IMethod) instructions[i - 1].Operand).Name == "op_Inequality"
					&& instructions[i].Is(Code.Brfalse_S)) {
					instructions.Insert(i + 1, this.Call(HandlePatchChat));
					instructions.Insert(i + 2, OpCodes.Brtrue_S.ToInstruction((Instruction) instructions[i].Operand));
					return;
				}
			}
			throw new ArgumentException("Couldn't find code to replace.");
		}
	}

	internal class PlayerInputPatch : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(PlayerInput), "DebugKeys");
		public static void Prefix(List<Keys> keys) => CommandManager.HandleInput(keys);
	}
}
