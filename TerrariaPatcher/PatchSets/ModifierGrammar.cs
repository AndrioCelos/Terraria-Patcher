#nullable enable

using System;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Terraria;

namespace TerrariaPatcher.PatchSets;

internal class ModifierGrammar : PatchSet {
	public override string Name => "Modifier Grammar";
	public override Version Version => new(1, 0);
	public override string Description => "Tweaks the way modifiers are applied to certain English item names, such as the Breaker.";

	internal class ModifierGrammarPatch : Patch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(Item), nameof(Item.AffixName));
		public override void PatchMethodBody(MethodDef method) {
			// Replace the last call to string.Concat(string, string, string).
			var instructions = method.Body.Instructions;
			for (int i = instructions.Count - 1; i >= 0; i--) {
				if (instructions[i].Is(Code.Call) && ((IMethod) instructions[i].Operand).Name == nameof(string.Concat)) {
					instructions[i] = this.Call(ApplyModifierToName);
					return;
				}
			}
			throw new ArgumentException("Couldn't find code to replace.");
		}

		public static string ApplyModifierToName(string modifier, string space, string itemName)
			=> itemName.StartsWith("The ") ? $"The {modifier} {itemName.Substring(4)}" : $"{modifier} {itemName}";
	}
}
