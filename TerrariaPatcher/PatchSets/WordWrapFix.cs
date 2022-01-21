#nullable enable

using System;
using System.Collections.Generic;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

using ReLogic.Graphics;

using Terraria;
using Terraria.UI.Chat;

namespace TerrariaPatcher.PatchSets;

internal class WordWrapFix : PatchSet {
	public override string Name => "Word Wrap Fix";
	public override Version Version => new(1, 0);
	public override string Description => "Modifies the chat word wrap logic to work properly.";

	internal class WordWrapPatch : Patch {
		public override PatchTarget TargetMethod => PatchTarget.Create(Terraria.Utils.WordwrapStringSmart);

		internal static void WordWrapLineSmarter(DynamicSpriteFont font, int maxWidth, List<TextSnippet> linePart, int l, ref float x, ref float width, out int pos) {
			string[] words = linePart[l].Text.Split(' ');
			if (words.Length > 1) {
				pos = 0;
				for (var m = 0; m < words.Length; m++) {
					width = font.MeasureString(words[m] + ' ').X * linePart[l].Scale;
					if (m > 0 && width + x > maxWidth)
						break;
					pos += words[m].Length + 1;
					x += width;
				}
				if (pos > linePart[l].Text.Length) pos = linePart[l].Text.Length;
			} else
				pos = linePart[l].Text.Length;
		}

		public override void PatchMethodBody(MethodDef method) {
			var instructions = method.Body.Instructions;
			for (int i = 1; i < instructions.Count - 2; i++) {
				if (instructions[i + 1].IsConstant(0f)
					&& instructions[i + 2].Is(Code.Ble_Un_S)) {

					var local_linePart = GetLocalCheckType(method, 8, "List`1", nameof(TextSnippet));  // Decompiled as list3
					var local_l = GetLocalCheckType(method, 10, nameof(Int32), null);
					var local_x = GetLocalCheckType(method, 9, nameof(Single), null);  // Decompiled as num
					var local_width = GetLocalCheckType(method, 11, nameof(Single), null);  // Decompiled as stringLength
					var local_pos = GetLocalCheckType(method, 15, nameof(Int32), null);  // Decompiled as num4

					// Delete instructions from `if (num > 0f)` and before `string newText = list3[l].Text.Substring(0, num4);`
					while (!(instructions[i].IsLdloc()
						&& instructions[i + 1].IsLdloc()
						&& instructions[i + 2].Is(Code.Callvirt) && ((IMethod) instructions[i + 2].Operand).Name == "get_Item"
						&& instructions[i + 3].Is(Code.Ldfld) && ((IField) instructions[i + 3].Operand).Name == nameof(TextSnippet.Text)
						&& instructions[i + 4].IsConstant(0)
						&& instructions[i + 5].IsLdloc() && instructions[i + 5].Operand == local_pos
						&& instructions[i + 6].Is(Code.Callvirt) && ((IMethod) instructions[i + 6].Operand).Name == nameof(string.Substring))) {
						instructions.RemoveAt(i);
					}

					// Now add our code.
					instructions.Insert(i, OpCodes.Ldarg_2.ToInstruction());
					instructions.Insert(i + 1, OpCodes.Ldarg_3.ToInstruction());
					instructions.Insert(i + 2, OpCodes.Ldloc_S.ToInstruction(local_linePart));
					instructions.Insert(i + 3, OpCodes.Ldloc_S.ToInstruction(local_l));
					instructions.Insert(i + 4, OpCodes.Ldloca_S.ToInstruction(local_x));
					instructions.Insert(i + 5, OpCodes.Ldloca_S.ToInstruction(local_width));
					instructions.Insert(i + 6, OpCodes.Ldloca_S.ToInstruction(local_pos));
					instructions.Insert(i + 7, Call(WordWrapLineSmarter));

					return;
				}
			}
			throw new ArgumentException("Couldn't find code to replace.");
		}

		[NoCopyToTarget]
		public static Local GetLocalCheckType(MethodDef method, int localIndex, string typeName, string? genericArgName) {
			var local = method.Body.Variables[localIndex];
			return local.Type.TypeName == typeName &&
				(genericArgName is not null ? (local.Type is GenericInstSig sig && sig.GenericArguments[0].TypeName == genericArgName) : !local.Type.IsGenericInstanceType)
				? local
				: throw new ArgumentException($"Type of local {localIndex} ({local.Type}) did not match the expected ({typeName})");
		}
	}

	internal class ChatMessageContainerPatch : Patch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(ChatMessageContainer), nameof(ChatMessageContainer.Refresh));

		public override void PatchMethodBody(MethodDef method) {
			// Replace `Main.screenWidth - 320` with `(int) (Main.screenWidth / Main.UIScale - 176 * Main.UIScale)`.
			var instructions = method.Body.Instructions;
			for (int i = 1; i < instructions.Count; i++) {
				if (instructions[i - 1].Is(Code.Ldsfld) && ((IField) instructions[i - 1].Operand).Name == nameof(Main.screenWidth)
					&& instructions[i].IsConstant(320)) {

					instructions.RemoveAt(i);
					instructions.RemoveAt(i);
					instructions.Insert(i + 0, OpCodes.Conv_R4.ToInstruction());
					instructions.Insert(i + 1, OpCodes.Call.ToInstruction(method.Module.Import(typeof(Main)).ResolveTypeDefThrow().FindProperty(nameof(Main.UIScale)).GetMethod));
					instructions.Insert(i + 2, OpCodes.Div.ToInstruction());
					instructions.Insert(i + 3, OpCodes.Ldc_R4.ToInstruction(150f));
					instructions.Insert(i + 4, OpCodes.Call.ToInstruction(method.Module.Import(typeof(Main)).ResolveTypeDefThrow().FindProperty(nameof(Main.UIScale)).GetMethod));
					instructions.Insert(i + 5, OpCodes.Mul.ToInstruction());
					instructions.Insert(i + 6, OpCodes.Sub.ToInstruction());
					instructions.Insert(i + 7, OpCodes.Conv_I4.ToInstruction());

					return;
				}
			}
			throw new ArgumentException("Couldn't find code to replace.");
		}
	}
}
