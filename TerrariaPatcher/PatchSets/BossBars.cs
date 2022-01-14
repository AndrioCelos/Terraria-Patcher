#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using dnlib.DotNet;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using ReLogic.Content;

using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.ID;
using Terraria.UI.Chat;

namespace TerrariaPatcher.PatchSets;

internal class BossBars : PatchSet {
	public override string Name => "Better Boss Bars";
	public override Version Version => new(1, 0);
	public override string Description => "Adds text to boss bars with the name of the boss and its exact life remaining, and adds a boss bar for the Eternia Crystal.";

	internal class BossBarSystemPatch : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(BigProgressBarSystem), "Draw");
		public static void Prefix(SpriteBatch spriteBatch) {
			// Draw the Eternia Crystal bar alongside an possible actual boss bar.
			var index = NPC.FindFirstNPC(NPCID.DD2EterniaCrystal);
			if (index >= 0) {
				var npc = Main.npc[index];
				var texture = TextureAssets.Item[ItemID.DD2ElderCrystal].Value;
				var barIconFrame = texture.Frame();
				var life = Terraria.GameContent.Events.DD2Event.LostThisRun ? 0 : npc.life;
				DrawFancyBar(spriteBatch, texture, barIconFrame, npc.FullName, 0, 0, life, npc.lifeMax, new(0, -50), new(2, 4));
			}
		}
	}

	private static void DrawBarFill(SpriteBatch spriteBatch, Texture2D bossBarTexture, Rectangle rectangle, int frame, int value, int max) {
		const int verticalFrames = 6;
		var barOffset = new Point(32, 24);

		var fillWidth = (int) (rectangle.Width * ((float) value / max));
		fillWidth -= fillWidth % 2;

		var fillFrame = bossBarTexture.Frame(verticalFrames: verticalFrames, frameY: frame + 1 );
		fillFrame.Offset(barOffset);
		fillFrame.Width = 2;
		fillFrame.Height = rectangle.Height;
		var shieldEndFrame = bossBarTexture.Frame(verticalFrames: verticalFrames, frameY: frame);
		shieldEndFrame.Offset(barOffset);
		shieldEndFrame.Width = 2;
		shieldEndFrame.Height = rectangle.Height;

		spriteBatch.Draw(bossBarTexture, rectangle.TopLeft(), fillFrame, Color.White, 0, Vector2.Zero, new Vector2(fillWidth / fillFrame.Width, 1), SpriteEffects.None, 0);
		spriteBatch.Draw(bossBarTexture, rectangle.TopLeft() + new Vector2(fillWidth - 2, 0), shieldEndFrame, Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
	}
	public static void DrawFancyBar(SpriteBatch spriteBatch, Texture2D barIconTexture, Rectangle barIconFrame, string name, int life, int maxLife, int shield, int maxShield, Point extraOffset = default, Point extraHeadOffset = default) {
		var bossBarTexture = Main.Assets.Request<Texture2D>("Images/UI/UI_BossBar", AssetRequestMode.ImmediateLoad).Value;
		const int verticalFrames = 6;
		var barOffset = new Point(32, 24);
		var backgroundSpriteRect = bossBarTexture.Frame(verticalFrames: verticalFrames, frameY: 3);
		var backgroundColor = Color.White * 0.2f;

		var rectangle = Utils.CenteredRectangle(Main.ScreenSize.ToVector2() * new Vector2(0.5f, 1) + new Vector2(0, -50), new(456, 22));
		rectangle.Offset(extraOffset);
		var borderTopLeft = rectangle.TopLeft() - barOffset.ToVector2();
		spriteBatch.Draw(bossBarTexture, borderTopLeft, backgroundSpriteRect, backgroundColor, 0f, Vector2.Zero, 1, SpriteEffects.None, 0f);

		if (life > 0 && maxLife > 0)
			DrawBarFill(spriteBatch, bossBarTexture, rectangle, 1, life, maxLife);
		if (shield > 0 && maxShield > 0)
			DrawBarFill(spriteBatch, bossBarTexture, rectangle, 4, shield, maxShield);

		var borderFrame = bossBarTexture.Frame(verticalFrames: verticalFrames, frameY: 0);
		spriteBatch.Draw(bossBarTexture, borderTopLeft, borderFrame, Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
		var iconOffset = new Vector2(4f, 20f) + barIconFrame.Size() / 2f + extraHeadOffset.ToVector2();
		spriteBatch.Draw(barIconTexture, borderTopLeft + iconOffset, barIconFrame, Color.White, 0f, barIconFrame.Size() / 2f, 1f, SpriteEffects.None, 0f);

		var text = (shield > 0 || maxLife == 0) ? $"{name}: {shield} / {maxShield}" : $"{name}: {life} / {maxLife}";
		var font = FontAssets.MouseText.Value;
		ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, text, new Vector2((Main.ScreenSize.X - font.MeasureString(text).X) / 2, Main.ScreenSize.Y - 62) + extraOffset.ToVector2(), Color.White, 0, Vector2.Zero, Vector2.One, -1, 2);
	}
	public static void DrawFancyBar(SpriteBatch spriteBatch, Texture2D barIconTexture, Rectangle barIconFrame, string name, int life, int maxLife)
		=> DrawFancyBar(spriteBatch, barIconTexture, barIconFrame, name, life, maxLife, 0, 0);

	internal static int GetNpcMaxLife(int npcType, NPC targetNpc, NPC dummyNpc) {
		dummyNpc.SetDefaults(npcType, targetNpc.GetMatchingSpawnParams());
		return dummyNpc.lifeMax;
	}

	internal class DrawFancyBarPatch1 : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(BigProgressBarHelper), "DrawFancyBar", typeof(SpriteBatch), typeof(float), typeof(Texture2D), typeof(Rectangle));
		public static bool Prefix(SpriteBatch spriteBatch, float lifePercent, Texture2D barIconTexture, Rectangle barIconFrame) {
			DrawFancyBar(spriteBatch, barIconTexture, barIconFrame, "*", (int) (lifePercent * 1000), 1000);
			return Program.SKIP_ORIGINAL;
		}
	}

	internal class DrawFancyBarPatch2 : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(BigProgressBarHelper), "DrawFancyBar", typeof(SpriteBatch), typeof(float), typeof(Texture2D), typeof(Rectangle), typeof(float));
		public static bool Prefix(SpriteBatch spriteBatch, float lifePercent, Texture2D barIconTexture, Rectangle barIconFrame, float shieldPercent) {
			DrawFancyBar(spriteBatch, barIconTexture, barIconFrame, "*", (int) (lifePercent * 1000), 1000, (int) (shieldPercent * 1000), 1000);
			return Program.SKIP_ORIGINAL;
		}
	}

	internal class CommonBossDraw : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(CommonBossBigProgressBar), "Draw");
		public static bool Prefix(ref BigProgressBarInfo info, SpriteBatch spriteBatch, int ____headIndex) {
			var value = TextureAssets.NpcHeadBoss[____headIndex].Value;
			var barIconFrame = value.Frame();
			var npc = Main.npc[info.npcIndexToAimAt];
			DrawFancyBar(spriteBatch, value, barIconFrame, npc.FullName, npc.life, npc.lifeMax);
			return Program.SKIP_ORIGINAL;
		}
	}

	internal class LunarPillarDraw : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(LunarPillarBigProgessBar), "Draw");
		public static bool Prefix(ref BigProgressBarInfo info, SpriteBatch spriteBatch, int ____headIndex) {
			var shield = ____headIndex switch {
				NPCID.LunarTowerNebula => NPC.ShieldStrengthTowerNebula,
				NPCID.LunarTowerSolar => NPC.ShieldStrengthTowerSolar,
				NPCID.LunarTowerStardust => NPC.ShieldStrengthTowerStardust,
				NPCID.LunarTowerVortex => NPC.ShieldStrengthTowerVortex,
				_ => 0
			};
			var texture = TextureAssets.NpcHeadBoss[NPCID.Sets.BossHeadTextures[____headIndex]].Value;
			var barIconFrame = texture.Frame();
			var npc = Main.npc[info.npcIndexToAimAt];
			DrawFancyBar(spriteBatch, texture, barIconFrame, npc.TypeName, npc.life, npc.lifeMax, shield, NPC.ShieldStrengthTowerMax);
			return Program.SKIP_ORIGINAL;
		}
	}

	internal class BrainOfCthulhuDraw : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(BrainOfCthuluBigProgressBar), "Draw");
		public static bool Prefix(ref BigProgressBarInfo info, SpriteBatch spriteBatch, NPC ____creeperForReference) {
			var npc = Main.npc[info.npcIndexToAimAt];
			var life = npc.life + (from npc2 in Main.npc where npc2.active && npc2.type == NPCID.Creeper
								   select npc2.life).Sum();
			var maxLife = npc.lifeMax + ____creeperForReference.lifeMax * NPC.GetBrainOfCthuluCreepersCount();
			var texture = TextureAssets.NpcHeadBoss[NPCID.Sets.BossHeadTextures[NPCID.BrainofCthulhu]].Value;
			DrawFancyBar(spriteBatch, texture, texture.Frame(), npc.TypeName, life, maxLife);
			return Program.SKIP_ORIGINAL;
		}
	}

	internal class EaterOfWorldsDraw : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(EaterOfWorldsProgressBar), "Draw");
		public static bool Prefix(ref BigProgressBarInfo info, SpriteBatch spriteBatch) {
			var npc = Main.npc[info.npcIndexToAimAt];
			var life = (from npc2 in Main.npc where npc2.active && npc2.type is NPCID.EaterofWorldsHead or NPCID.EaterofWorldsBody or NPCID.EaterofWorldsTail
						select npc2.life).Sum();
			var maxLife = npc.lifeMax * (NPC.GetEaterOfWorldsSegmentsCount() + 2);
			var texture = TextureAssets.NpcHeadBoss[NPCID.Sets.BossHeadTextures[NPCID.EaterofWorldsHead]].Value;
			DrawFancyBar(spriteBatch, texture, texture.Frame(), npc.TypeName, life, maxLife);
			return Program.SKIP_ORIGINAL;
		}
	}

	internal class GolemDraw : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(GolemHeadProgressBar), "Draw");
		public static bool Prefix(ref BigProgressBarInfo info, SpriteBatch spriteBatch, NPC ____referenceDummy) {
			var npc = Main.npc[info.npcIndexToAimAt];
			var life = (from npc2 in Main.npc where npc2.active && npc2.type is NPCID.Golem or NPCID.GolemHead
						select npc2.life).Sum();
			var maxLife = GetNpcMaxLife(NPCID.Golem, npc, ____referenceDummy)
				+ GetNpcMaxLife(NPCID.GolemHead, npc, ____referenceDummy);
			var texture = TextureAssets.NpcHeadBoss[NPCID.Sets.BossHeadTextures[NPCID.GolemHead]].Value;
			DrawFancyBar(spriteBatch, texture, texture.Frame(), npc.TypeName, life, maxLife);
			return Program.SKIP_ORIGINAL;
		}
	}

	internal class MartianSaucerDraw : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(MartianSaucerBigProgressBar), "Draw");
		public static bool Prefix(ref BigProgressBarInfo info, SpriteBatch spriteBatch, NPC ____referenceDummy) {
			var npc = Main.npc[info.npcIndexToAimAt];
			var life = (from npc2 in Main.npc where npc2.active && (npc2.type is NPCID.MartianSaucerTurret or NPCID.MartianSaucerCannon || (npc2.type == NPCID.MartianSaucerCore && Main.expertMode))
						select npc2.life).Sum();
			var maxLife = GetNpcMaxLife(NPCID.MartianSaucerTurret, npc, ____referenceDummy) * 2
				+ GetNpcMaxLife(NPCID.MartianSaucerCannon, npc, ____referenceDummy) * 2
				+ (Main.expertMode ? GetNpcMaxLife(NPCID.MartianSaucerCore, npc, ____referenceDummy) : 0);
			var texture = TextureAssets.NpcHeadBoss[NPCID.Sets.BossHeadTextures[NPCID.MartianSaucerCore]].Value;
			DrawFancyBar(spriteBatch, texture, texture.Frame(), npc.TypeName, life, maxLife);
			return Program.SKIP_ORIGINAL;
		}
	}

	internal class MoonLordDraw : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(MoonLordProgressBar), "Draw");
		public static bool Prefix(ref BigProgressBarInfo info, SpriteBatch spriteBatch, NPC ____referenceDummy) {
			var npc = Main.npc[info.npcIndexToAimAt];
			var life = (from npc2 in Main.npc where npc2.active && npc2.type is NPCID.MoonLordHead or NPCID.MoonLordHand or NPCID.MoonLordCore
						select npc2.life).Sum();
			var maxLife = GetNpcMaxLife(NPCID.MoonLordHead, npc, ____referenceDummy)
				+ GetNpcMaxLife(NPCID.MoonLordHand, npc, ____referenceDummy) * 2
				+ GetNpcMaxLife(NPCID.MoonLordCore, npc, ____referenceDummy);
			var texture = TextureAssets.NpcHeadBoss[NPCID.Sets.BossHeadTextures[NPCID.MoonLordHead]].Value;
			DrawFancyBar(spriteBatch, texture, texture.Frame(), npc.TypeName, life, maxLife);
			return Program.SKIP_ORIGINAL;
		}
	}

	internal class PirateShipDraw : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(PirateShipBigProgressBar), "Draw");
		public static bool Prefix(ref BigProgressBarInfo info, SpriteBatch spriteBatch, NPC ____referenceDummy) {
			var npc = Main.npc[info.npcIndexToAimAt];
			var life = (from npc2 in Main.npc where npc2.active && npc2.type is NPCID.PirateShipCannon
						select npc2.life).Sum();
			var maxLife = GetNpcMaxLife(NPCID.PirateShipCannon, npc, ____referenceDummy) * 4;
			var texture = TextureAssets.NpcHeadBoss[NPCID.Sets.BossHeadTextures[NPCID.PirateShip]].Value;
			DrawFancyBar(spriteBatch, texture, texture.Frame(), npc.TypeName, life, maxLife);
			return Program.SKIP_ORIGINAL;
		}
	}

	internal class TwinsDraw : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(TwinsBigProgressBar), "Draw");
		public static bool Prefix(ref BigProgressBarInfo info, SpriteBatch spriteBatch, int ____headIndex) {
			var npc = Main.npc[info.npcIndexToAimAt];
			var texture = TextureAssets.NpcHeadBoss[____headIndex].Value;
			DrawFancyBar(spriteBatch, texture, texture.Frame(), npc.TypeName, npc.life, npc.lifeMax);
			return Program.SKIP_ORIGINAL;
		}
	}
}
