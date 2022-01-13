using System;
using System.Collections.Generic;

using Terraria;
using Terraria.Audio;

using TerrariaPatcher.Mods;

namespace TerrariaPatcher.PatchSets;

internal class AccessorySwitchCommands : PatchSet {
	public override string Name => "Accessory Switch Commands";
	public override Version Version => new(1, 0);
	public override string Description => "Client-side commands to toggle accessory switches";
	public override IReadOnlyCollection<Type> Dependencies => new[] { typeof(Commands) };

	internal class InitializePatch : MainInitializePatch {
		public static void Postfix() {
			CommandManager.Commands.Add("ruler", GetAccessorySwitchCommand(0, "on", "off"));
			CommandManager.Commands.Add("mechanicalruler", GetAccessorySwitchCommand(1, "on", "off"));
			CommandManager.Commands.Add("presserator", GetAccessorySwitchCommand(2, "on", "off"));
			CommandManager.Commands.Add("paintsprayer", GetAccessorySwitchCommand(3, "on", "off"));
			CommandManager.Commands.Add("redwire", GetAccessorySwitchCommand(4, "bright", "normal", "faded", "hidden"));
			CommandManager.Commands.Add("bluewire", GetAccessorySwitchCommand(5, "bright", "normal", "faded", "hidden"));
			CommandManager.Commands.Add("greenwire", GetAccessorySwitchCommand(6, "bright", "normal", "faded", "hidden"));
			CommandManager.Commands.Add("yellowwire", GetAccessorySwitchCommand(7, "bright", "normal", "faded", "hidden"));
			CommandManager.Commands.Add("wiremode", GetAccessorySwitchCommand(8, "forced", "normal"));
			CommandManager.Commands.Add("actuators", GetAccessorySwitchCommand(9, "bright", "normal", "faded", "hidden"));
		}
	}

	private static CommandAction GetAccessorySwitchCommand(int index, params string[] keywords)
		=> args => {
			if (args[0].Equals("toggle", StringComparison.CurrentCultureIgnoreCase)) {
				Main.player[Main.myPlayer].builderAccStatus[index]++;
				if (Main.player[Main.myPlayer].builderAccStatus[index] >= keywords.Length)
					Main.player[Main.myPlayer].builderAccStatus[index] = 0;
			} else {
				var i = Array.FindIndex(keywords, s => args[0].Equals(s, StringComparison.CurrentCultureIgnoreCase));
				if (i < 0) {
					CommandManager.FailMessage($"Usage: [command] toggle|{string.Join("|", keywords)}");
					return;
				}
				Main.player[Main.myPlayer].builderAccStatus[index] = i;
				SoundEngine.PlaySound(12);
			}
		};
}
