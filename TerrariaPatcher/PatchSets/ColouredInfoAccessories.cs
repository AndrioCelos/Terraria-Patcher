#nullable enable

using System;
using System.Linq;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

using Terraria;

namespace TerrariaPatcher.PatchSets;

internal static class ColouredInfoAccessoriesHelper {
	private static Local? infoColourLocal;
	public static Local InfoColourLocal {
		get {
			// The Terraria assembly must be loaded here.
			if (infoColourLocal is not null) return infoColourLocal;

			var method = PatchTarget.Create(typeof(Main), "DrawInfoAccs").GetMethodDefs().Single();
			var instructions = method.Body.Instructions;
			for (int i = 5; i < instructions.Count; i++) {
				if (instructions[i - 4].Is(Code.Ldsfld) && ((IField) instructions[i - 4].Operand).Name == nameof(Main.mouseTextColor)
					&& instructions[i - 3].Is(Code.Ldsfld) && ((IField) instructions[i - 3].Operand).Name == nameof(Main.mouseTextColor)
					&& instructions[i - 2].Is(Code.Ldsfld) && ((IField) instructions[i - 2].Operand).Name == nameof(Main.mouseTextColor)
					&& instructions[i - 1].Is(Code.Ldsfld) && ((IField) instructions[i - 1].Operand).Name == nameof(Main.mouseTextColor)
					&& instructions[i].Is(Code.Call)) {
					if (instructions[i - 5].Operand is Local local) {
						infoColourLocal = local;
						return local;
					}
				}
			}
			throw new InvalidOperationException("Can't find infoTextColor local.");
		}
	}

	private static Local? infoShadowColourLocal;
	public static Local InfoShadowColourLocal {
		get {
			// The Terraria assembly must be loaded here.
			if (infoShadowColourLocal is not null) return infoShadowColourLocal;

			var method = PatchTarget.Create(typeof(Main), "DrawInfoAccs").GetMethodDefs().Single();
			var instructions = method.Body.Instructions;
			for (int i = 1; i < instructions.Count; i++) {
				if (instructions[i - 1].Is(Code.Call) && ((IMethod) instructions[i - 1].Operand).Name == "get_Black"
					&& instructions[i].Is(Code.Stloc_S)) {
					if (instructions[i].Operand is Local local) {
						infoShadowColourLocal = local;
						return local;
					}
				}
			}
			throw new InvalidOperationException("Can't find infoTextColor local.");
		}
	}
}
