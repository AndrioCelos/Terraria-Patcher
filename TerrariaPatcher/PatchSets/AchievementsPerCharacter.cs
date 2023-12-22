#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Terraria;
using Terraria.Achievements;
using Terraria.IO;

namespace TerrariaPatcher.PatchSets;

internal class AchievementsPerCharacter : PatchSet {
	public override string Name => "Achievements Per Character";
	public override Version Version => new(1, 0);
	public override string Description => "Separates achievement completion for each character.";
	public override IReadOnlyCollection<Type>? Dependencies => new[] { typeof(RemoveSteamAchievements) };

	internal class SetAsActivePatch : PrefixPatch {
		private static readonly Lazy<FieldInfo> AchievementsSavePathField = new(() => typeof(AchievementManager).GetField("_savePath", BindingFlags.NonPublic | BindingFlags.Instance));

		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(PlayerFileData), nameof(PlayerFileData.SetAsActive));
		public static void Postfix(PlayerFileData __instance) {
			// Clear all achievements without saving.
			foreach (var achievement in Main.Achievements.CreateAchievementsList())
				achievement.ClearProgress();

			var achievementsPath = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(__instance.Path)), "Achievements", $"{Path.GetFileNameWithoutExtension(__instance.Path)}.dat");
			Directory.CreateDirectory(Path.GetDirectoryName(achievementsPath));
			AchievementsSavePathField.Value.SetValue(Main.Achievements, achievementsPath);
			if (File.Exists(achievementsPath)) Main.Achievements.Load();
		}
	}
}
