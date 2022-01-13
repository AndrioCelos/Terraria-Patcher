#nullable enable

using System;
using System.Collections.Generic;

using ReLogic.Graphics;

using Terraria;
using Terraria.GameContent;
using Terraria.UI;

using TerrariaPatcher.Mods;

namespace TerrariaPatcher.PatchSets;

internal class PauseCommand : PatchSet {
	public override string Name => "Pause Command";
	public override Version Version => new(1, 0);
	public override string Description => "Adds a client-side command to pause the game.";
	public override IReadOnlyCollection<Type> Dependencies => new[] { typeof(Commands) };

	public static bool IsPaused;

	internal class InitializePatch : MainInitializePatch {
		public static void Postfix() {
			CommandManager.Commands.Add("pause", args => {
				if (Main.netMode != 0) {
					Main.NewText("You can't pause in multiplayer.", 192, 64, 64);
					return;
				}
				IsPaused = args.Length > 0 && args[0].Equals("toggle", StringComparison.CurrentCultureIgnoreCase)
					? !IsPaused : true;
			});
			CommandManager.Commands.Add("unpause", args => IsPaused = false);
		}
	}

	internal class OldOnesArmyCountdownPatch : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(Main), "CanPauseGame");

		public static void Postfix(ref bool __result) {
			if (Main.netMode == 0 && IsPaused) __result = true;
		}
	}

	internal class SetupDrawInterfaceLayersPatch : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(Main), "SetupDrawInterfaceLayers");

		public static void Prefix(bool ____needToSetupDrawInterfaceLayers, out bool __state)
			=> __state = ____needToSetupDrawInterfaceLayers;

		public static void Postfix(bool __state, List<GameInterfaceLayer> ____gameInterfaceLayers) {
			if (__state) {
				____gameInterfaceLayers.Add(new LegacyGameInterfaceLayer("Mods: Pause text", () => {
					if (IsPaused)
						Main.spriteBatch.DrawString(FontAssets.DeathText.Value, "Paused", new(Main.screenWidth / 2 - FontAssets.DeathText.Value.MeasureString("Paused").X / 2f, Main.screenHeight / 2 - 20), new(255, 240, 20, 128));
					return true;
				}, InterfaceScaleType.UI));
			}
		}
	}
}
