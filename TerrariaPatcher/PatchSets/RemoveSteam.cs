#nullable enable

using System;

using Terraria.Social;

namespace TerrariaPatcher.PatchSets;

internal class RemoveSteam : PatchSet {
	public override string Name => "_Remove Steam";
	public override Version Version => new(1, 0);
	public override string Description => "Disables the Steam integration.";

	internal class RemoveSteamPatch : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(SocialAPI.Initialize);
		public static void Prefix(ref SocialMode? mode) => mode ??= SocialMode.None;
	}
}
