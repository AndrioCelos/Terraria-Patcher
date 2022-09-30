#nullable enable

using System;
using System.Drawing;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

using TerrariaPatcher.Mods;

namespace TerrariaPatcher.PatchSets;

internal class ModManagerMod : PatchSet {
	public override string Name => "Mod Manager";
	public override Version Version => new(1, 0);
	public override string Description => "Provides code used by other mods.";

	public override void BeforeApply() {
		var typeDef = ImportType(typeof(ModManager), "Terraria");
		if (this.Config is ConfigFile config) {
			var method = typeDef.FindStaticConstructor();
			var replacements = 0;
			var instructions = method.Body.Instructions;
			for (int i = 5; i < instructions.Count; i++) {
				if (instructions[i].Is(Code.Stsfld)
					&& instructions[i - 1].Is(Code.Newobj)
					&& instructions[i - 2].OpCode.Code is Code.Ldc_I4 or Code.Ldc_I4_S
					&& instructions[i - 3].OpCode.Code is Code.Ldc_I4 or Code.Ldc_I4_S
					&& instructions[i - 4].OpCode.Code is Code.Ldc_I4 or Code.Ldc_I4_S
					&& instructions[i - 5].OpCode.Code is Code.Ldc_I4 or Code.Ldc_I4_S) {
					Color color;
					switch (((IField) instructions[i].Operand).Name) {
						case nameof(ModManager.AccentColor): color = config.AccentColor; break;
						case nameof(ModManager.SuccessColor): color = config.SuccessColor; break;
						case nameof(ModManager.FailColor): color = config.FailColor; break;
						default: continue;
					}
					instructions[i - 5] = OpCodes.Ldc_I4.ToInstruction((int) color.R);
					instructions[i - 4] = OpCodes.Ldc_I4.ToInstruction((int) color.G);
					instructions[i - 3] = OpCodes.Ldc_I4.ToInstruction((int) color.B);
					instructions[i - 2] = OpCodes.Ldc_I4.ToInstruction((int) color.A);
					replacements++;
				}
			}
			if (replacements == 0) throw new ArgumentException("Couldn't find code to replace.");
		}
	}

	public class ConfigFile : IPatchSetConfig {
		public Color AccentColor { get; set; } = Color.FromArgb(ModManager.AccentColor.A, ModManager.AccentColor.R, ModManager.AccentColor.G, ModManager.AccentColor.B);
		public Color SuccessColor { get; set; } = Color.FromArgb(ModManager.SuccessColor.A, ModManager.SuccessColor.R, ModManager.SuccessColor.G, ModManager.SuccessColor.B);
		public Color FailColor { get; set; } = Color.FromArgb(ModManager.FailColor.A, ModManager.FailColor.R, ModManager.FailColor.G, ModManager.FailColor.B);
	}
}
