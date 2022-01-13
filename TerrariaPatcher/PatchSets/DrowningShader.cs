#nullable enable

using System;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

using Microsoft.Xna.Framework;

using Terraria.GameContent.RGB;

namespace TerrariaPatcher.PatchSets;

internal class DrowningShaderMod : PatchSet {
	public override string Name => "Drowning RGB Lighting";
	public override Version Version => new(1, 0);
	public override string Description => "Changes the drowning shader gradient to horizontal instead of vertical.";

	internal class DrowningShaderPatch : Patch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(DrowningShader), "ProcessHighDetail");

		public override void PatchMethodBody(MethodDef method) {
			// Replaces `_breath * 1.2f - 0.1f` with `_breath * 5 - 1`.
			// Remove the `* 5` from `(canvasPositionOfIndex.Y - num) * 5` and replace Y with X.
			var instructionsRemoved = 0; bool replaced1Point2 = false, replacedY = false;
			var instructions = method.Body.Instructions;
			for (int i = 0; i < instructions.Count; i++) {
				if (instructions[i].IsConstant(1.2f)) {
					instructions[i].Operand = 5f;
					replaced1Point2 = true;
				} else if (instructions[i].IsConstant(0.1f)) {
					instructions[i].Operand = 1f;
				} else if (instructions[i].IsConstant(5f)) {
					instructions.RemoveAt(i);
					instructions.RemoveAt(i);
					instructionsRemoved += 2;
					i--;
				} else if (instructions[i].Is(Code.Ldfld) && ((IField) instructions[i].Operand).Name == nameof(Vector2.Y)) {
					var memberRef = (MemberRef) instructions[i].Operand;
					instructions[i].Operand = new MemberRefUser(memberRef.Module, nameof(Vector2.X), memberRef.FieldSig, memberRef.Class);
					replacedY = true;
				}
			}
			if (instructionsRemoved == 0 || !replaced1Point2 || !replacedY) throw new ArgumentException("Couldn't find code to replace.");
		}
	}
}
