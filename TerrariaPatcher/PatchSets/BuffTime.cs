#nullable enable

using System;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

using Terraria;

namespace TerrariaPatcher.PatchSets;

internal class BuffTime : PatchSet {
	public override string Name => "Precise Buff Times";
	public override Version Version => new(1, 0);
	public override string Description => "Changes the buff icons to show minutes and seconds, or tenth-seconds. Also affects the Invasion Progress patch during Old One's Army intermissions.";

	internal class DrawBuffIconPatch : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(Main.DrawBuffIcon);

		public override void PatchMethodBody(MethodDef method) {
			// Replace `Lang.LocalizedDuration(new(0, 0, buffTimeValue / 60), abbreviated: true, showAllAvailableUnits: false)`
			// with `Lang.LocalizedDuration(new(0, 0, 0, 0, buffTimeValue / 3 * 50), abbreviated: true, showAllAvailableUnits: true)`
			var instructions = method.Body.Instructions;
			for (int i = 5; i < instructions.Count; i++) {
				if (instructions[i - 5].IsLdloc()
					&& instructions[i - 4].IsConstant(60)
					&& instructions[i - 3].Is(Code.Div)
					&& instructions[i - 2].Is(Code.Newobj)
					&& instructions[i - 1].IsConstant(1)
					&& instructions[i].IsConstant(0)) {
					// The first two zeroes are already on the stack.
					i -= 5;
					instructions.Insert(i, OpCodes.Ldc_I4_0.ToInstruction());
					instructions.Insert(i + 1, OpCodes.Ldc_I4_0.ToInstruction());
					instructions[i + 3].Operand = (sbyte) 3;
					instructions.Insert(i + 5, OpCodes.Ldc_I4_S.ToInstruction((sbyte) 50));
					instructions.Insert(i + 6, OpCodes.Mul.ToInstruction());

					var int32 = currentPatchTargetModule.ModuleDef.CorLibTypes.Int32;
					instructions[i + 7].Operand = new MemberRefUser(
						currentPatchTargetModule.ModuleDef,
						".ctor",
						MethodSig.CreateInstance(currentPatchTargetModule.ModuleDef.CorLibTypes.Void, int32, int32, int32, int32, int32),
						currentPatchTargetModule.ModuleDef.CorLibTypes.GetTypeRef("System", "TimeSpan"));
					
					instructions[i + 9] = OpCodes.Ldc_I4_1.ToInstruction();
					return;
				}
			}
			throw new ArgumentException("Couldn't find code to replace.");
		}
	}

	internal class LangPatch : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(Lang.LocalizedDuration);
		public static bool Prefix(TimeSpan time, bool showAllAvailableUnits, ref string __result) {
			if (showAllAvailableUnits) {
				__result = time.Days > 0 ? $"{time.Days}:{time.Hours}d"
					: time.Hours > 0 ? $"{time.Hours}:{time.Minutes}h"
					: time.Minutes > 0 ? $"{time.Minutes}'{time.Seconds}\""
					: $"{time.Seconds}.{time.Milliseconds / 100}\"";
				return Program.SKIP_ORIGINAL;
			}
			return Program.DONT_SKIP_ORIGINAL;
		}
	}
}
