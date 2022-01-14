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
		public static void Prefix() {
			CommandManager.Commands.Add("hotbar", new(CommandHotbar, 1, 1, "toggle/unlock/lock", "Toggles the hotbar lock."));

			CommandManager.Commands.Add("ruler", GetAccessorySwitchCommand(0, "Toggles the ruler display.", "on", "off"));
			CommandManager.Commands.Add("mechanicalruler", GetAccessorySwitchCommand(1, "Toggles the mechanical ruler display.", "on", "off"));
			CommandManager.Commands.Add("presserator", GetAccessorySwitchCommand(2, "Toggles the presserator.", "on", "off"));
			CommandManager.Commands.Add("paintsprayer", GetAccessorySwitchCommand(3, "Toggles the paint sprayer.", "on", "off"));
			CommandManager.Commands.Add("redwire", GetAccessorySwitchCommand(4, "Changes the red wire display style.", "bright", "normal", "faded", "hidden"));
			CommandManager.Commands.Add("bluewire", GetAccessorySwitchCommand(5, "Changes the blue wire display style.", "bright", "normal", "faded", "hidden"));
			CommandManager.Commands.Add("greenwire", GetAccessorySwitchCommand(6, "Changes the green wire display style.", "bright", "normal", "faded", "hidden"));
			CommandManager.Commands.Add("yellowwire", GetAccessorySwitchCommand(7, "Changes the yellow wire display style.", "bright", "normal", "faded", "hidden"));
			CommandManager.Commands.Add("wiremode", GetAccessorySwitchCommand(8, "Toggles forced wire display.", "forced", "normal"));
			CommandManager.Commands.Add("actuators", GetAccessorySwitchCommand(9, "Changes the actuator display.", "bright", "normal", "faded", "hidden"));
			CommandManager.Commands.Add("blockswap", GetAccessorySwitchCommand(10, "Toggles block swap.", "on", "off"));
			CommandManager.Commands.Add("torchswap", GetAccessorySwitchCommand(11, "Toggles biome torch conversion.", "on", "off"));
		}
	}

	private static void CommandHotbar(Command command, string label, string[] args) {
		if (args[0].Equals("toggle", StringComparison.CurrentCultureIgnoreCase)) {
			Main.player[Main.myPlayer].hbLocked = !Main.player[Main.myPlayer].hbLocked;
		} else if (args[0].Equals("unlock", StringComparison.CurrentCultureIgnoreCase)) {
			Main.player[Main.myPlayer].hbLocked = false;
		} else if (args[0].Equals("lock", StringComparison.CurrentCultureIgnoreCase)) {
			Main.player[Main.myPlayer].hbLocked = true;
		} else {
			command.ShowParametersFailMessage(label);
			return;
		}
		SoundEngine.PlaySound(Terraria.ID.SoundID.Unlock);
	}

	private static Command GetAccessorySwitchCommand(int index, string description, params string[] keywords)
		=> new((command, label, args) => {
			if (args[0].Equals("toggle", StringComparison.CurrentCultureIgnoreCase)) {
				Main.player[Main.myPlayer].builderAccStatus[index]++;
				if (Main.player[Main.myPlayer].builderAccStatus[index] >= keywords.Length)
					Main.player[Main.myPlayer].builderAccStatus[index] = 0;
			} else {
				var i = Array.FindIndex(keywords, s => args[0].Equals(s, StringComparison.CurrentCultureIgnoreCase));
				if (i < 0) {
					command.ShowParametersFailMessage(label);
					return;
				}
				Main.player[Main.myPlayer].builderAccStatus[index] = i;
			}
			SoundEngine.PlaySound(Terraria.ID.SoundID.MenuTick);
		}, 1, 1, $"toggle|{string.Join("/", keywords)}", description);
}
