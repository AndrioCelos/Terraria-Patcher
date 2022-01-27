#nullable enable

using System;

using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.UI;
using Terraria;

namespace TerrariaPatcher.PatchSets;

internal class ForTheWorthyBestiaryMod : PatchSet {
	public override string Name => "For the Worthy Bestiary Mod";
	public override Version Version => new(1, 0);
	public override string Description => "Unlocks the Bunny or Demon bestiary entries in For the Worthy worlds when the Explosive Bunny or Voodoo Demon entries are unlocked respectively.";

	public class ForTheWorthyCollectionInfoProvider : IBestiaryUICollectionInfoProvider {
		private readonly string creditID;
		private readonly string substituteCreditID;

		public ForTheWorthyCollectionInfoProvider(string creditID, string substituteCreditID) {
			this.creditID = creditID ?? throw new ArgumentNullException(nameof(creditID));
			this.substituteCreditID = substituteCreditID ?? throw new ArgumentNullException(nameof(substituteCreditID));
		}
		public ForTheWorthyCollectionInfoProvider(int npcID, int substituteNpcID)
			: this(ContentSamples.NpcBestiaryCreditIdsByNpcNetIds[npcID], ContentSamples.NpcBestiaryCreditIdsByNpcNetIds[substituteNpcID]) { }

		public BestiaryUICollectionInfo GetEntryUICollectionInfo() {
			var creditID = Main.getGoodWorld ? this.substituteCreditID : this.creditID;
			return new BestiaryUICollectionInfo() {
				UnlockState = CommonEnemyUICollectionInfoProvider.GetUnlockStateByKillCount(Main.BestiaryTracker.Kills.GetKillCount(creditID), false)
			};
		}

		public UIElement? ProvideUIElement(BestiaryUICollectionInfo _) => null;
	}

	public class ForTheWorthyBestiaryPatch : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(BestiaryDatabaseNPCsPopulator), "ModifyEntriesThatNeedIt");

		public static void Postfix(BestiaryDatabase ____currentDatabase) {
			____currentDatabase.FindEntryByNPCID(NPCID.Bunny).UIInfoProvider = new ForTheWorthyCollectionInfoProvider(NPCID.Bunny, NPCID.ExplosiveBunny);
			____currentDatabase.FindEntryByNPCID(NPCID.Demon).UIInfoProvider = new ForTheWorthyCollectionInfoProvider(NPCID.Demon, NPCID.VoodooDemon);
		}
	}
}
