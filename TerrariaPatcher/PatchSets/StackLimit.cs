#nullable enable

using System;
using System.Collections.Generic;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

using ReLogic.Graphics;

using Terraria;
using Terraria.GameContent;
using Terraria.UI;

using TerrariaPatcher.Mods;

namespace TerrariaPatcher.PatchSets;

internal class StackLimitMod : PatchSet {
	public override string Name => "Stack Limit";
	public override Version Version => new(1, 1);
	public override string Description => "Adds a client command to limit the number of items to buy, duplicate or split off a stack.";
	public override IReadOnlyCollection<Type>? Dependencies => new[] { typeof(Commands), typeof(ModManagerMod) };

	public static int StackLimit = int.MaxValue;

	internal class InitializePatch : MainInitializePatch {
		public static void Prefix()
			=> CommandManager.Commands.Add("stack", new((command, label, args) => {
				if (args.Length == 0) {
					StackLimit = int.MaxValue;
					return;
				}
				if (int.TryParse(args[0], out var n) && n >= 0)
					StackLimit = n;
				else
					command.ShowParametersFailMessage(label);
			}, 0, 1, "[n]", "Limits the next purchase, duplication or stack split to the specified number of items, or removes an existing limit."));
	}

	internal class LeftClickPatch : Patch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(ItemSlot), "LeftClick", typeof(Item[]), typeof(int), typeof(int));

		public override void PatchMethodBody(MethodDef method) {
			// Replace `Main.mouseItem.maxStack` with `Math.Min(Main.mouseItem.maxStack, StackLimit)`.
			var instructions = method.Body.Instructions;
			for (int i = 1; i < instructions.Count; i++) {
				if (instructions[i - 1].Is(Code.Ldfld) && ((IField) instructions[i - 1].Operand).Name == nameof(Item.maxStack)
					&& instructions[i].Is(Code.Stfld) && ((IField) instructions[i].Operand).Name == nameof(Item.stack)) {
					instructions.Insert(i, LoadField(typeof(StackLimitMod), nameof(StackLimit)));
					instructions.Insert(i + 1, Call(typeof(Math), nameof(Math.Min), MethodSig.CreateStatic(currentPatchTargetModule.CurrentModuleDef.CorLibTypes.Int32, currentPatchTargetModule.CurrentModuleDef.CorLibTypes.Int32, currentPatchTargetModule.CurrentModuleDef.CorLibTypes.Int32)));
					return;
				}
			}
			throw new ArgumentException($"Couldn't find a position to insert code.");
		}
	}

	internal abstract class RightClickPatch : Patch {
		public override void PatchMethodBody(MethodDef method) {
			var instructions = method.Body.Instructions;
			for (int i = 3; i < instructions.Count; i++) {
				if (instructions[i].Is(Code.Blt_S) || instructions[i].Is(Code.Bge_S)) {
					// Replace `Main.mouseItem.maxStack` with `Math.Min(Main.mouseItem.maxStack, StackLimit)`
					// in `Main.mouseItem.stack < Main.mouseItem.maxStack`.
					if (instructions[i - 4].Is(Code.Ldsfld) && ((IField) instructions[i - 4].Operand).Name == nameof(Main.mouseItem)
						&& instructions[i - 3].Is(Code.Ldfld) && ((IField) instructions[i - 3].Operand).Name == nameof(Item.stack)
						&& instructions[i - 2].Is(Code.Ldsfld) && ((IField) instructions[i - 2].Operand).Name == nameof(Main.mouseItem)
						&& instructions[i - 1].Is(Code.Ldfld) && ((IField) instructions[i - 1].Operand).Name == nameof(Item.maxStack)) {
						instructions.Insert(i, LoadField(typeof(StackLimitMod), nameof(StackLimit)));
						instructions.Insert(i + 1, Call(typeof(Math), nameof(Math.Min), MethodSig.CreateStatic(currentPatchTargetModule.CurrentModuleDef.CorLibTypes.Int32, currentPatchTargetModule.CurrentModuleDef.CorLibTypes.Int32, currentPatchTargetModule.CurrentModuleDef.CorLibTypes.Int32)));
						return;
					}
				}
			}
			throw new ArgumentException($"Couldn't find a position to insert code.");
		}
	}
	internal class RightClickPatch1 : RightClickPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(ItemSlot), "RightClick", typeof(Item[]), typeof(int), typeof(int));
	}
	internal class RightClickPatch2 : RightClickPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(ItemSlot), "HandleShopSlot");
	}

	internal class TryAllowingToCraftRecipePatch : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(Main), "TryAllowingToCraftRecipe");

		public static bool Prefix(Recipe currentRecipe, ref bool __result) {
			if (!Main.mouseItem.IsAir && Main.mouseItem.IsTheSameAs(currentRecipe.createItem)
				&& Main.mouseItem.stack + currentRecipe.createItem.stack <= Main.mouseItem.maxStack) {
				__result = Main.mouseItem.stack + currentRecipe.createItem.stack <= StackLimit;
				return Program.SKIP_ORIGINAL;
			}
			return Program.DONT_SKIP_ORIGINAL;
		}
	}

	internal class SetupDrawInterfaceLayersPatch : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(Main), "SetupDrawInterfaceLayers");

		public static void Prefix(bool ____needToSetupDrawInterfaceLayers, out bool __state)
			=> __state = ____needToSetupDrawInterfaceLayers;

		public static void Postfix(bool __state, List<GameInterfaceLayer> ____gameInterfaceLayers) {
			if (__state) {
				____gameInterfaceLayers.Add(new LegacyGameInterfaceLayer("Mods: Stack limit", delegate () {
					if (StackLimit < 2147483647)
						Main.spriteBatch.DrawString(FontAssets.MouseText.Value, $"Count: {StackLimit}", new(88, Main.screenHeight - 30), ModManager.AccentColor);
					return true;
				}, InterfaceScaleType.UI));
			}
		}
	}

	internal class DrawPatch : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(Main), "DoDraw");

		public static void Prefix(out int __state)
			=> __state = (Main.mouseLeftRelease ? 1 : 0) | (Main.mouseRightRelease ? 2 : 0);

		public static void Postfix(int __state) {
			// If either 'mouse button released' flag has just been set, reset StackCount.
			var state2 = (Main.mouseLeftRelease ? 1 : 0) | (Main.mouseRightRelease ? 2 : 0);
			if ((state2 & ~__state) != 0 && Main.mouseItem.stack >= StackLimit)
				StackLimit = int.MaxValue;
		}
	}
}
