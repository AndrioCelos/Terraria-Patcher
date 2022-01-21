#nullable enable

using System;

using Terraria.Social;

namespace TerrariaPatcher.PatchSets;

internal class RemoveSteamAchievements : PatchSet {
	public override string Name => "Remove Steam Achievements";
	public override Version Version => new(1, 0);
	public override string Description => "Disables integration of Steam achivements.";

	internal class RemoveSteamAchievementsPatch : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(SocialAPI), "LoadSteam");
		public static void Postfix() => SocialAPI.Achievements = null;
	}
}
