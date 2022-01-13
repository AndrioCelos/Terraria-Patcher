#nullable enable

using System;

using dnlib.DotNet;

using Terraria.UI;

namespace TerrariaPatcher.PatchSets;

internal class ItemSlotGlow : PatchSet {
	public override string Name => "Item Slot Glow";
	public override Version Version => new(1, 0);
	public override string Description => "Increases the intensity and duration of the colour fade effect when sorting the inventory.";

	internal class ItemSlotGlowPatch : Patch {
		public override PatchTarget TargetMethod => PatchTarget.Create(ItemSlot.SetGlow);

		public override void PatchMethodBody(MethodDef method) {
			foreach (var instruction in method.Body.Instructions) {
				if (instruction.IsConstant(300))
					instruction.Operand = 600;
			}
		}
	}
}
