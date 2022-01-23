#nullable enable

using System;

using Terraria;

namespace TerrariaPatcher.PatchSets;

internal class ShootFix : PatchSet {
	public override string Name => "Shoot Fix";
	public override Version Version => new(1, 1);
	public override string Description => "Fixes an off-by-one issue causing certain weapons' animation and actual shooting to desync.";

	internal class ShootFixPatch : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(Player), "SetItemAnimation");

		public static void Prefix(ref int frames) {
			// When auto-firing shooting weapons such as staves, itemAnimation is set before both variables are decremented,
			// but itemTime is set after. This causes the animation and shooting to gradually go out of sync.
			frames++;
		}
	}
}
