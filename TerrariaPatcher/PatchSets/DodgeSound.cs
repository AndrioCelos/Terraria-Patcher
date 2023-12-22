#nullable enable

using System;

using Terraria;
using Terraria.Audio;

namespace TerrariaPatcher.PatchSets;

internal class DodgeSound : PatchSet {
	public override string Name => "Dodge Sound";
	public override Version Version => new(1, 0);
	public override string Description => "Adds a sound effect when you dodge an attack using a Brain of Confusion, Hallowed armour, or ninja gear.";

	internal class DodgePatch1 : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(Player), nameof(Player.NinjaDodge));
		public static void Postfix(Player __instance) {
			if (__instance.whoAmI == Main.myPlayer)
				SoundEngine.PlaySound(Terraria.ID.SoundID.DoubleJump, __instance.position);
		}
	}

	internal class DodgePatch2 : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(Player), nameof(Player.BrainOfConfusionDodge));
		public static void Postfix(Player __instance) {
			if (__instance.whoAmI == Main.myPlayer)
				SoundEngine.PlaySound(Terraria.ID.SoundID.DoubleJump, __instance.position);
		}
	}

	internal class DodgePatch3 : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(Player), nameof(Player.ShadowDodge));
		public static void Postfix(Player __instance) {
			if (__instance.whoAmI == Main.myPlayer)
				SoundEngine.PlaySound(Terraria.ID.SoundID.DoubleJump, __instance.position);
		}
	}
}
