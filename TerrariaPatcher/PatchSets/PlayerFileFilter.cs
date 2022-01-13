#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.IO;
using Terraria.Social.Steam;

using TerrariaPatcher.Mods;

namespace TerrariaPatcher.PatchSets;

internal class PlayerFileFilter : PatchSet {
	public override string Name => "Player File Filter";
	public override Version Version => new(1, 0);
	public override string Description => "Filters the character list with the name of a Steam friend you are joining.";
	public override IReadOnlyCollection<Type>? Dependencies => new[] { typeof(Mods) };

	public static string? FilterString;

	public static void SetFilter(string text) {
		if (text is null) {
			FilterString = null;
			return;
		}
		var stringBuilder = new StringBuilder();
		var invalidFileNameChars = Path.GetInvalidFileNameChars();
		foreach (var c in text) {
			if (c is not (' ' or '*' or '.')) invalidFileNameChars.Contains(c);
			stringBuilder.Append(c);
		}
		FilterString = stringBuilder.ToString();
	}

	internal class LoadPlayersPatch : Patch {
		public override PatchTarget TargetMethod => PatchTarget.Create(Main.LoadPlayers);

		public static string Filter => $"*{FilterString}.plr";

		public override void PatchMethodBody(MethodDef method) {
			// Replace "*.plr" with `Filter`.
			var instructions = method.Body.Instructions;
			for (int i = 0; i < instructions.Count; i++) {
				if (instructions[i].IsConstant("*.plr")) {
					instructions[i] = Call(typeof(LoadPlayersPatch), "get_" + nameof(Filter));
					return;
				}
			}
			throw new ArgumentException("Couldn't find code to replace.");
		}
	}

	internal class LoadPlayersLambdaPatch : Patch {
		// This currently cannot be a lambda because it would cause the lambda to be imported to the target assembly.
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(Main), GetTargetMethod);
		private MethodDef GetTargetMethod(TypeDef type) {
			foreach (var t in type.NestedTypes) {
				foreach (var m in t.Methods) {
					if (m.ReturnType.ElementType == ElementType.Boolean && m.Name.Contains(nameof(Main.LoadPlayers)))
						return m;
				}
			}
			throw new MissingMethodException("Can't find LoadPlayers lambda method.");
		}

		public static string Filter => $"{FilterString}.plr";

		public override void PatchMethodBody(MethodDef method) {
			// Replace "*.plr" with `Filter`.
			var instructions = method.Body.Instructions;
			for (int i = 0; i < instructions.Count; i++) {
				if (instructions[i].IsConstant(".plr")) {
					instructions[i] = Call(typeof(LoadPlayersLambdaPatch), "get_" + nameof(Filter));
					return;
				}
			}
			throw new ArgumentException("Couldn't find code to replace.");
		}
	}

	internal class GetPlayerPathFromNamePatch : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(Main.GetPlayerPathFromName);

		public static void Prefix(ref string playerName) {
			if (FilterString is not null)
				playerName = $"{playerName}-{FilterString}";
		}
	}

	internal class DrawMenuPatch : Patch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(Main), "DrawMenu");

		public override void PatchMethodBody(MethodDef method) {
			// Insert `Terraria.Mods.Mods.PlayerFileFilter = null` before each `LoadPlayers()` call.
			var replaced = 0;
			var instructions = method.Body.Instructions;
			for (int i = 0; i < instructions.Count; i++) {
				// (These don't have labels.)
				if (instructions[i].Is(Code.Call) && ((IMethod) instructions[i].Operand).Name == nameof(Main.LoadPlayers)) {
					instructions.Insert(i, OpCodes.Ldnull.ToInstruction());
					instructions.Insert(i + 1, StoreField(typeof(PlayerFileFilter), nameof(FilterString)));
					i += 2;
					replaced++;
				}
			}
			if (replaced < 2) throw new ArgumentException($"Expected 2 replacements but made {replaced}.");
		}
	}

	internal class NetClientSocialModulePatch : Patch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(NetClientSocialModule), "OnLobbyJoinRequest");

		public override void PatchMethodBody(MethodDef method) {
			// Insert `SetFilter(friendName)` intercepting the value before assigning it.
			var instructions = method.Body.Instructions;
			for (int i = 0; i < instructions.Count; i++) {
				if (instructions[i].Is(Code.Stfld) && ((IField) instructions[i].Operand).Name == "friendName") {
					instructions.Insert(i, OpCodes.Dup.ToInstruction());
					instructions.Insert(i + 1, Call(PlayerFileFilter.SetFilter));
					return;
				}
			}
			throw new ArgumentException("Couldn't find code to replace.");
		}
	}

	internal class UICharacterListItemPatch : Patch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(UICharacterListItem), "DrawSelf");

		public static void AlterText(ref string text, PlayerFileData data) {
			var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(data.GetFileName(true));
			if (fileNameWithoutExtension.StartsWith(data.Name + "-")) {
				var color = ModManager.AccentColor;
				text += $"[c/{color.R:X2}{color.G:X2}{color.B:X2}:{fileNameWithoutExtension.Substring(text.Length)}]";
			}
		}

		public override void PatchMethodBody(MethodDef method) {
			// Insert a call to AlterText immediately before `if (this._data.Player.loadStatus != 0)`.
			var foundNameAccess = false;
			var instructions = method.Body.Instructions;
			for (int i = 0; i < instructions.Count; i++) {
				if (!foundNameAccess) {
					if (instructions[i].Is(Code.Ldfld) && ((IField) instructions[i].Operand).Name == "Name")
						foundNameAccess = true;
				} else {
					if (instructions[i].IsStloc()) {
						var instruction = instructions[i].OpCode.Code switch {
							Code.Stloc_0 => OpCodes.Ldloca_S.ToInstruction(0),
							Code.Stloc_1 => OpCodes.Ldloca_S.ToInstruction(1),
							Code.Stloc_2 => OpCodes.Ldloca_S.ToInstruction(2),
							Code.Stloc_3 => OpCodes.Ldloca_S.ToInstruction(3),
							_ => OpCodes.Ldloca_S.ToInstruction((Local) instructions[i].Operand)
						};
						instructions.Insert(i + 1, instruction);
						instructions.Insert(i + 2, OpCodes.Ldarg_0.ToInstruction());
						instructions.Insert(i + 3, LoadField(typeof(UICharacterListItem), "_data"));
						instructions.Insert(i + 4, Call(AlterText));
						return;
					}
				}
			}
			throw new ArgumentException("Couldn't find code to replace.");
		}
	}
}
