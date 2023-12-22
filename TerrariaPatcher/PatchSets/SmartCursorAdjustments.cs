#nullable enable

using System;
using System.ComponentModel;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace TerrariaPatcher.PatchSets;

internal class SmartCursorAdjustments : PatchSet {
	public override string Name => "Smart Cursor Adjustments";
	public override Version Version => new(1, 0);
	public override string Description => "Several minor adjustments to Smart Cursor targeting.";

	[NoCopyToTarget]
	private static IMethod? AbsMethod;

	internal class PickaxeThreeWideVerticalDigPatch : Patch {

		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(SmartCursorHelper), "Step_Pickaxe_MineSolids");

		public override void PatchMethodBody(MethodDef method) {
			if (this.PatchSet?.Config is ConfigFile config && !config.PickaxeThreeWideVerticalDig) return;
			var instructions = method.Body.Instructions;
			OpCode? setWidthLocalCode = null;
			Local? positionLocal = null;
			var i = 1;

			AbsMethod = (from inst in instructions where inst.Is(Code.Call) select (IMethod) inst.Operand into m where m.Name == nameof(Math.Abs) select m).First();

			for (; i < instructions.Count; i++) {
				if (instructions[i - 1].Is(Code.Ldfld) && ((IField) instructions[i - 1].Operand).Name == nameof(Entity.width)
					&& instructions[i].IsStloc()) {
					setWidthLocalCode = instructions[i].OpCode;
					break;
				}
			}
			if (setWidthLocalCode is null)
				throw new ArgumentException("Couldn't find 'player width' local variable.");

			for (; i < instructions.Count; i++) {
				if (instructions[i - 1].Is(Code.Ldfld) && ((IField) instructions[i - 1].Operand).Name == nameof(Entity.position)
					&& instructions[i].Is(Code.Stloc_S)) {
					positionLocal = (Local) instructions[i].Operand;
					break;
				}
			}
			if (positionLocal is null)
				throw new ArgumentException("Couldn't find 'player position' local variable.");

			for (i++; i < instructions.Count; i++) {
				if (instructions[i - 6].Is(Code.Stloc_S)
					&& instructions[i - 2].Is(Code.Ldfld) && ((IField) instructions[i - 2].Operand).Name == nameof(Vector2.X)
					&& instructions[i - 1].Is(Code.Stfld) && ((IField) instructions[i - 1].Operand).Name == nameof(Vector2.X)) {
					var nextInstruction = instructions[i];
					var vectorXFieldRef = (IField) instructions[i - 1].Operand;
					
					// Insert code to treat the player as three tiles wide if they are intersecting three tile columns and aiming straight up or down (within ~18 degrees).
					instructions.Insert(i + 0, OpCodes.Ldloc_S.ToInstruction(positionLocal));
					instructions.Insert(i + 1, OpCodes.Ldfld.ToInstruction(vectorXFieldRef));
					instructions.Insert(i + 2, OpCodes.Ldc_R4.ToInstruction(16f));
					instructions.Insert(i + 3, OpCodes.Rem.ToInstruction());
					instructions.Insert(i + 4, OpCodes.Ldc_R4.ToInstruction(12f));
					instructions.Insert(i + 5, OpCodes.Ble_Un_S.ToInstruction(nextInstruction));
					instructions.Insert(i + 6, OpCodes.Ldc_I4_S.ToInstruction((sbyte) 34));
					instructions.Insert(i + 7, setWidthLocalCode.ToInstruction());
					instructions.Insert(i + 8, OpCodes.Ldloca_S.ToInstruction(positionLocal));
					instructions.Insert(i + 9, OpCodes.Ldflda.ToInstruction(vectorXFieldRef));
					instructions.Insert(i + 10, OpCodes.Dup.ToInstruction());
					instructions.Insert(i + 11, OpCodes.Ldind_R4.ToInstruction());
					instructions.Insert(i + 12, OpCodes.Ldc_R4.ToInstruction(7f));
					instructions.Insert(i + 13, OpCodes.Sub.ToInstruction());
					instructions.Insert(i + 14, OpCodes.Stind_R4.ToInstruction());

					return;
				}
			}
			throw new ArgumentException("Couldn't find where to insert code.");
		}
	}

	internal class PlatformsHorizontalPatch : Patch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(SmartCursorHelper), "Step_Platforms");

		public override void PatchMethodBody(MethodDef method) {
			if (this.PatchSet?.Config is ConfigFile config && !config.PlatformsHorizontal) return;
			var instructions = method.Body.Instructions;
			var i = 2;

			var providedInfoTypeDef = method.Parameters[0].Type.TryGetTypeDef();
			var mouseField = providedInfoTypeDef.FindField("mouse");
			var playerField = providedInfoTypeDef.FindField("player");
			var vector2TypeSig = mouseField.FieldType;
			var vector2TypeRef = mouseField.FieldType.ToTypeDefOrRef();
			var mouseDeltaLocal = method.Body.Variables.Add(new(vector2TypeSig, "mouseDelta", method.Body.Variables.Count));
			var yFieldRef = new MemberRefUser(vector2TypeSig.Module, "Y", new FieldSig(vector2TypeSig.Module.CorLibTypes.Single), vector2TypeRef);

			for (; i < instructions.Count; i++) {
				if (instructions[i - 2].Is(Code.Ldloc_0)
					&& instructions[i - 1].Is(Code.Brtrue)) {
					var nextInstruction = instructions[i];

					// Get the displacement of the mouse cursor from the player.
					instructions.Insert(i + 0, (Instruction?) OpCodes.Ldarg_0.ToInstruction());
					instructions.Insert(i + 1, OpCodes.Ldfld.ToInstruction(mouseField));
					instructions.Insert(i + 2, OpCodes.Ldarg_0.ToInstruction());
					instructions.Insert(i + 3, OpCodes.Ldfld.ToInstruction(playerField));
					instructions.Insert(i + 4, this.Call(typeof(Entity), $"get_{nameof(Entity.Bottom)}"));
					instructions.Insert(i + 5, OpCodes.Call.ToInstruction(new MemberRefUser(vector2TypeSig.Module, "op_Subtraction", new MethodSig(CallingConvention.Default, 0, vector2TypeSig, vector2TypeSig, vector2TypeSig), vector2TypeRef)));
					instructions.Insert(i + 6, OpCodes.Stloc_S.ToInstruction(mouseDeltaLocal));

					// Check whether it is horizontal within ~22.5 degrees.
					instructions.Insert(i + 7, OpCodes.Ldloc_S.ToInstruction(mouseDeltaLocal));
					instructions.Insert(i + 8, OpCodes.Ldfld.ToInstruction(new MemberRefUser(vector2TypeSig.Module, "X", new FieldSig(vector2TypeSig.Module.CorLibTypes.Single), vector2TypeRef)));
					instructions.Insert(i + 9, OpCodes.Call.ToInstruction(AbsMethod ?? throw new InvalidOperationException("Abs method not found")));
					instructions.Insert(i + 10, OpCodes.Ldloc_S.ToInstruction(mouseDeltaLocal));
					instructions.Insert(i + 11, OpCodes.Ldfld.ToInstruction(yFieldRef));
					instructions.Insert(i + 12, OpCodes.Call.ToInstruction(AbsMethod));
					instructions.Insert(i + 13, OpCodes.Ldc_R4.ToInstruction(2.4f));
					instructions.Insert(i + 14, OpCodes.Mul.ToInstruction());
					instructions.Insert(i + 15, OpCodes.Ble_Un_S.ToInstruction(nextInstruction));

					instructions.Insert(i + 16, OpCodes.Ldc_I4_1.ToInstruction());
					instructions.Insert(i + 17, OpCodes.Stloc_0.ToInstruction());

					break;
				}
			}

			var replacements = 4;
			for (i += 18; i < instructions.Count; i++) {
				if (instructions[i].Is(Code.Beq_S) && instructions[i - 1].OpCode.Code is Code.Ldc_I4_1 or Code.Ldc_I4_2) {
					var branchTarget = (Instruction) instructions[i].Operand;

					// If the mouse is pointing horizontally, do not consider placing platforms diagonally adjacent to existing tiles.
					instructions.Insert(i + 1, OpCodes.Ldloc_0.ToInstruction());
					instructions.Insert(i + 2, OpCodes.Brtrue_S.ToInstruction(branchTarget));

					replacements--;
					if (replacements == 0) return;
				}
			}
			throw new ArgumentException("Couldn't find where to insert code.");
		}
	}

	internal class AlchemySeedsDoNotReplaceOtherPlantsPatch : Patch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(SmartCursorHelper), "Step_AlchemySeeds");

		public override void PatchMethodBody(MethodDef method) {
			if (this.PatchSet?.Config is ConfigFile config && !config.AlchemySeedsDoNotReplaceOtherPlants) return;
			var instructions = method.Body.Instructions;

			var providedInfoTypeDef = method.Parameters[0].Type.TryGetTypeDef();
			var itemField = providedInfoTypeDef.FindField("item");

			var i = 4;
			for (; i < instructions.Count; i++) {
				if (instructions[i - 4].Is(Code.Beq_S)
					&& instructions[i - 3].Is(Code.Ldloc_3)
					&& instructions[i - 2].Is(Code.Ldfld)
					&& instructions[i - 1].Is(Code.Ldc_I4_S) && (sbyte) instructions[i - 1].Operand == TileID.MatureHerbs
					&& instructions[i].Is(Code.Bne_Un_S)) {
					var falseBranchTarget = (Instruction) instructions[i - 4].Operand;

					instructions.Insert(i + 0, OpCodes.Beq_S.ToInstruction(falseBranchTarget));
					instructions.Insert(i + 1, OpCodes.Ldloc_3.ToInstruction());
					instructions.Insert(i + 2, OpCodes.Ldfld.ToInstruction((IField) instructions[i - 2].Operand));
					instructions.Insert(i + 3, OpCodes.Ldc_I4_S.ToInstruction((sbyte) TileID.BloomingHerbs));

					break;
				}
			}
			for (i += 4; i < instructions.Count; i++) {
				if (instructions[i].Is(Code.Call) && ((IMethod) instructions[i].Operand).Name == nameof(WorldGen.IsHarvestableHerbWithSeed)) {
					instructions.Insert(i + 0, OpCodes.Ldarg_0.ToInstruction());
					instructions.Insert(i + 1, OpCodes.Ldfld.ToInstruction(itemField));
					instructions[i + 2] = this.Call(IsHarvestableHerbWithSeed);
					return;
				}
			}
			throw new ArgumentException("Couldn't find where to insert code.");
		}

		public static bool IsHarvestableHerbWithSeed(int type, int style, Item item) => item.placeStyle == style && WorldGen.IsHarvestableHerbWithSeed(type, style);
	}

	public class ConfigFile : IPatchSetConfig {
		[DisplayName("Pickaxe 3-Wide Dig")]
		[Description("If enabled, digging straight up or down while intersecting three time columns will dig a 3-wide shaft.")]
		public bool PickaxeThreeWideVerticalDig { get; set; } = true;
		[DisplayName("Horizontal Platforms")]
		[Description("Makes it easier to place platforms horizontally without making stairs.")]
		public bool PlatformsHorizontal { get; set; } = true;
		[DisplayName("Alchemy Seeds Adjustment")]
		[Description("If enabled, Smart Cursor will not place a seed over a different plant.")]
		public bool AlchemySeedsDoNotReplaceOtherPlants { get; set; } = true;
	}
}
