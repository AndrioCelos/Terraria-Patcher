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

using static Terraria.ID.ArmorIDs;

namespace TerrariaPatcher.PatchSets;

internal class BossBars : PatchSet {
	public override string Name => "Better Boss Bars";
	public override Version Version => new(1, 0);
	public override string Description => "Adds text to boss bars with the name of the boss and its exact life remaining, and adds a boss bar for the Eternia Crystal.";

	public override void AfterApply() {
		// Change EterniaCrystalBossBar's base type to an internal type.
		var targetModule = Program.GetTargetModule(this.TargetModuleName).ModuleDef;
		TypeDef? typeDef = null;
		foreach (var t in targetModule.Types) {
			if (t.FullName == "TerrariaPatcher.PatchSets.BossBars") {
				foreach (var t2 in t.NestedTypes) {
					if (t2.Name == nameof(EterniaCrystalBossBar)) {
						typeDef = t2;
						break;
					}
				}
				break;
			}
		}
		if (typeDef is null) throw new InvalidOperationException("Can't find EterniaCrystalBossBar.");

		TypeDef? interfaceTypeDef = null;
		foreach (var t in targetModule.Types) {
			if (t.FullName == "Terraria.GameContent.UI.BigProgressBar.IBigProgressBar") {
				interfaceTypeDef = t;
				break;
			}
		}
		if (interfaceTypeDef is null) throw new InvalidOperationException("Can't find IBigProgressBar.");

		typeDef.Attributes = TypeAttributes.NestedPublic | TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit;
		typeDef.Interfaces.Add(new InterfaceImplUser(interfaceTypeDef));

		foreach (var method in typeDef.Methods) {
			if (!method.Attributes.HasFlag(MethodAttributes.SpecialName)) {
				method.Attributes = MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual;
			}
		}
	}

	internal class BossBarSystemPatch : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Constructor(typeof(BigProgressBarSystem));
		public static void Postfix(IDictionary ____bossBarsByNpcNetId) => ____bossBarsByNpcNetId[(int) NPCID.DD2EterniaCrystal] = new EterniaCrystalBossBar();
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
	public static void DrawFancyBar(SpriteBatch spriteBatch, Texture2D barIconTexture, Rectangle barIconFrame, string name, int life, int maxLife, int shield, int maxShield, Point extraOffset = default) {
		var bossBarTexture = Main.Assets.Request<Texture2D>("Images/UI/UI_BossBar", AssetRequestMode.ImmediateLoad).Value;
		const int verticalFrames = 6;
		var barOffset = new Point(32, 24);
		var backgroundSpriteRect = bossBarTexture.Frame(verticalFrames: verticalFrames, frameY: 3);
		var backgroundColor = Color.White * 0.2f;

		var rectangle = Utils.CenteredRectangle(Main.ScreenSize.ToVector2() * new Vector2(0.5f, 1) + new Vector2(0, -50), new(456, 22));
		rectangle.Offset(extraOffset);
		var borderTopLeft = rectangle.TopLeft() - barOffset.ToVector2();
		spriteBatch.Draw(bossBarTexture, borderTopLeft, backgroundSpriteRect, backgroundColor, 0f, Vector2.Zero, 1, SpriteEffects.None, 0f);

		DrawBarFill(spriteBatch, bossBarTexture, rectangle, 1, life, maxLife);
		if (shield > 0 && maxShield > 0)
			DrawBarFill(spriteBatch, bossBarTexture, rectangle, 4, shield, maxShield);

		var borderFrame = bossBarTexture.Frame(verticalFrames: verticalFrames, frameY: 0);
		spriteBatch.Draw(bossBarTexture, borderTopLeft, borderFrame, Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
		var iconOffset = new Vector2(4f, 20f) + barIconFrame.Size() / 2f;
		spriteBatch.Draw(barIconTexture, borderTopLeft + iconOffset, barIconFrame, Color.White, 0f, barIconFrame.Size() / 2f, 1f, SpriteEffects.None, 0f);

		var text = (shield > 0) ? $"{name}: {shield} / {maxShield}" : $"{name}: {life} / {maxLife}";
		var font = FontAssets.MouseText.Value;
		ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, text, new Vector2((Main.ScreenSize.X - font.MeasureString(text).X) / 2, Main.ScreenSize.Y - 60) + extraOffset.ToVector2(), Color.White, 0, Vector2.Zero, Vector2.One, -1, 2);
	}
	public static void DrawFancyBar(SpriteBatch spriteBatch, Texture2D barIconTexture, Rectangle barIconFrame, string name, int life, int maxLife)
		=> DrawFancyBar(spriteBatch, barIconTexture, barIconFrame, name, life, maxLife, 0, 0);

	public static void DrawMultiNpc(IEnumerable<NPC> npcs, string name, int bossHeadIndex, SpriteBatch spriteBatch) {
		int life = 0, maxLife = 0;
		foreach (var npc in npcs) {
			life += npc.life;
			maxLife += npc.lifeMax;
		}
		var texture = TextureAssets.NpcHeadBoss[bossHeadIndex].Value;
		var barIconFrame = texture.Frame();
		DrawFancyBar(spriteBatch, texture, barIconFrame, name, life, maxLife);
	}
	public static void DrawMultiNpc(Predicate<(int index, NPC npc)> shouldCountNpcPredicate, string name, int bossHeadIndex, SpriteBatch spriteBatch)
		=> DrawMultiNpc(Enumerable.Range(0, Main.npc.Length).Select(i => (index: i, npc: Main.npc[i])).Where(e => e.npc.active && shouldCountNpcPredicate(e)).Select(e => e.npc),
			name, bossHeadIndex, spriteBatch);

	public class EterniaCrystalBossBar {
		private NPC? npc;

		public bool ValidateAndCollectNecessaryInfo(ref BigProgressBarInfo info) {
			if (this.npc is not null) {
				if (this.npc.active && this.npc.type == NPCID.DD2EterniaCrystal) return true;
			}
			this.npc = Main.npc.FirstOrDefault(n => n.active && n.type == NPCID.DD2EterniaCrystal);
			return this.npc is not null;
		}

		public void Draw(ref BigProgressBarInfo info, SpriteBatch spriteBatch) {
			var texture = TextureAssets.Item[ItemID.DD2ElderCrystal].Value;
			var barIconFrame = texture.Frame();
			DrawFancyBar(spriteBatch, texture, barIconFrame, this.npc.FullName, 0, this.npc.lifeMax, this.npc.life, this.npc.lifeMax, new(0, -50));
		}
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
			DrawFancyBar(spriteBatch, texture, barIconFrame, npc.FullName, npc.life, npc.lifeMax, shield, NPC.ShieldStrengthTowerMax);
			return Program.SKIP_ORIGINAL;
		}
	}

	internal class BrainOfCthulhuDraw : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(BrainOfCthuluBigProgressBar), "Draw");
		public static bool Prefix(ref BigProgressBarInfo info, SpriteBatch spriteBatch) {
			var npcIndex = info.npcIndexToAimAt;
			var npcName = Main.npc[info.npcIndexToAimAt].FullName;
			DrawMultiNpc(e => e.index == npcIndex || e.npc.type == NPCID.Creeper, npcName, NPCID.Sets.BossHeadTextures[NPCID.BrainofCthulhu], spriteBatch);
			return Program.SKIP_ORIGINAL;
		}
	}

	internal class EaterOfWorldsDraw : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(EaterOfWorldsProgressBar), "Draw");
		public static bool Prefix(SpriteBatch spriteBatch) {
			NPC? referenceSegment = null;
			var life = 0;
			foreach (var npc in Main.npc) {
				if (npc.active && npc.type is NPCID.EaterofWorldsHead or NPCID.EaterofWorldsBody or NPCID.EaterofWorldsTail) {
					referenceSegment = npc;
					life += npc.life;
				}
			}
			var maxLife = (referenceSegment?.lifeMax ?? 150) * NPC.GetEaterOfWorldsSegmentsCount();
			var texture = TextureAssets.NpcHeadBoss[NPCID.Sets.BossHeadTextures[NPCID.EaterofWorldsHead]].Value;
			var barIconFrame = texture.Frame();
			DrawFancyBar(spriteBatch, texture, barIconFrame, "Eater of Worlds", life, maxLife);
			return Program.SKIP_ORIGINAL;
		}
	}

	internal class GolemDraw : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(GolemHeadProgressBar), "Draw");
		public static bool Prefix(SpriteBatch spriteBatch) {
			DrawMultiNpc(e => e.npc.type is NPCID.Golem or NPCID.GolemHead, "Golem", NPCID.Sets.BossHeadTextures[NPCID.GolemHead], spriteBatch);
			return Program.SKIP_ORIGINAL;
		}
	}

	internal class MartianSaucerDraw : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(MartianSaucerBigProgressBar), "Draw");
		public static bool Prefix(SpriteBatch spriteBatch) {
			DrawMultiNpc(e => e.npc.type is NPCID.MartianSaucerTurret or NPCID.MartianSaucerCannon || (Main.expertMode && e.npc.type == NPCID.MartianSaucerCore), "Martian Saucer", NPCID.Sets.BossHeadTextures[NPCID.MartianSaucerCore], spriteBatch);
			return Program.SKIP_ORIGINAL;
		}
	}

	internal class MoonLordDraw : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(MoonLordProgressBar), "Draw");
		public static bool Prefix(SpriteBatch spriteBatch) {
			DrawMultiNpc(e => e.npc.type is NPCID.MoonLordHead or NPCID.MoonLordHand or NPCID.MoonLordCore, "Moon Lord", NPCID.Sets.BossHeadTextures[NPCID.MoonLordHead], spriteBatch);
			return Program.SKIP_ORIGINAL;
		}
	}

	internal class PirateShipDraw : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(PirateShipBigProgressBar), "Draw");
		public static bool Prefix(ref BigProgressBarInfo info, SpriteBatch spriteBatch) {
			var baseNpc = Main.npc[info.npcIndexToAimAt];
			var npcs = baseNpc.ai.Select(i => (int) i)
				.Where(i => i >= 0 && i < Main.npc.Length)
				.Select(i => Main.npc[i])
				.Where(n => n.active && n.type == NPCID.PirateShipCannon);
			DrawMultiNpc(npcs, "Flying Dutchman", NPCID.Sets.BossHeadTextures[NPCID.PirateShip], spriteBatch);
			return Program.SKIP_ORIGINAL;
		}
	}

	internal class TwinsDraw : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(TwinsBigProgressBar), "Draw");
		public static bool Prefix(SpriteBatch spriteBatch, int ____headIndex) {
			DrawMultiNpc(e => e.npc.type is NPCID.Retinazer or NPCID.Spazmatism, "The Twins", ____headIndex, spriteBatch);
			return Program.SKIP_ORIGINAL;
		}
	}
}
