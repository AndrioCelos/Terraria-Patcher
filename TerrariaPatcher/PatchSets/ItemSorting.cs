#nullable enable

using System;
using System.Collections.Generic;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

using Terraria;

namespace TerrariaPatcher.PatchSets;

internal class ItemSorting : PatchSet {
	public override string Name => "Item Sorting";
	public override Version Version => new(1, 0);
	public override string Description => "Changes item sorting so that some item types that used to compare as equal no longer do.";

	internal class ItemSortingPatch : Patch {
		public override PatchTarget TargetMethod => PatchTarget.Create("Terraria", "Terraria.UI.ItemSorting+ItemSortingLayers", GetTargetMethods);
		private IEnumerable<MethodDef> GetTargetMethods(TypeDef type) {
			foreach (var t in type.NestedTypes) {
				foreach (var m in t.Methods) {
					if (m.ReturnType.ElementType == ElementType.I4 && m.Parameters.Count == 3
						&& m.Parameters[1].Type.ElementType == ElementType.I4 && m.Parameters[2].Type.ElementType == ElementType.I4)
						yield return m;
				}
			}
		}

		public override void PatchMethodBody(MethodDef method) {
			// Replaces Item.stack with Item.type and swaps x and y in those expressions.
			var instructions = method.Body.Instructions;
			for (int i = 2; i < instructions.Count; i++) {
				if (instructions[i - 2].IsLdarg()
					&& instructions[i - 1].Is(Code.Ldelem_Ref)
					&& (instructions[i].Is(Code.Ldfld) || instructions[i].Is(Code.Ldflda))
					&& ((IField) instructions[i].Operand).Name == nameof(Item.stack)) {
					var fieldDef = (FieldDef) instructions[i].Operand;
					instructions[i].Operand = fieldDef.DeclaringType.GetField(nameof(Item.type));
 					instructions[i - 2].OpCode = instructions[i - 2].OpCode.Code switch {
						Code.Ldarg_1 => OpCodes.Ldarg_2,
						Code.Ldarg_2 => OpCodes.Ldarg_1,
						_ => throw new InvalidOperationException("Don't know what to do with this opcode")
					};
				}
			}
		}
	}
}
