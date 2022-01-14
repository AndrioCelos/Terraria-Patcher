#nullable enable

using System;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

using Microsoft.Xna.Framework;

using Terraria;
using Terraria.GameContent;
using Terraria.UI.Chat;

namespace TerrariaPatcher.PatchSets;

internal class MenuTitleSplash : PatchSet {
	public override string Name => "Menu Title Splash";
	public override Version Version => new(1, 0);
	public override string Description => "Adds a title splash to the menu, for full screen or borderless window players.";

	public static void DrawTitleSplash(string title) {
		if (title is not null) {
			var size = FontAssets.MouseText.Value.MeasureString(title);
			ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, FontAssets.MouseText.Value, title, new(Main.screenWidth / 2, 200),
				new Color(255, 240, 20) * ((float) Main.mouseTextColor / 255), 0, size / 2, new(1.5f, 1.5f));
		}
	}

	internal class MenuTitleSplashPatch : Patch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(Main), "DrawMenu");

		public override void PatchMethodBody(MethodDef method) {
			// Insert a call immediately after the logo is drawn.
			var instructions = method.Body.Instructions;
			for (int i = 0; i < instructions.Count; i++) {
				if (instructions[i].Is(Code.Ldsfld) && ((IField) instructions[i].Operand).Name == nameof(TextureAssets.Logo2)) {
					for (i++; i < instructions.Count; i++) {
						if (instructions[i].Is(Code.Ldsfld) && ((IField) instructions[i].Operand).Name == nameof(Main.dayTime)) {
							// Branches point to this instruction, so repoint them at the new instruction.
							var newRefInstruction = OpCodes.Ldarg_0.ToInstruction();

							for (var j = i - 1; j >= 0; j--) {
								if (instructions[j].Operand == instructions[i]) instructions[j].Operand = newRefInstruction;
							}

							instructions.Insert(i, newRefInstruction);
							instructions.Insert(i + 1, OpCodes.Ldfld.ToInstruction(method.Module.Import(typeof(Main)).ResolveTypeDefThrow().FindField("_cachedTitle")));
							instructions.Insert(i + 2, Call(MenuTitleSplash.DrawTitleSplash));

							return;
						}
					}
				}
			}
			throw new ArgumentException("Couldn't find code to replace.");
		}
	}
}
