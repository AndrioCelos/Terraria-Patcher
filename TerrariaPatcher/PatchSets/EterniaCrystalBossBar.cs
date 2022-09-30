#nullable enable

using System;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.ID;

namespace TerrariaPatcher.PatchSets;

internal class EterniaCrystalBossBar : PatchSet {
	public override string Name => "Eternia Crystal Boss Bar";
	public override Version Version => new(1, 3);
	public override string Description => "Adds a boss bar for the Eternia Crystal.";

	public static Point bossBarExtraOffset;

	internal static void ApplyExtraOffset(ref Rectangle rectangle) {
		rectangle.Offset(bossBarExtraOffset);
		bossBarExtraOffset = default;
	}

	internal class BossBarSystemPatch : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(BigProgressBarSystem), "Draw");
		public static void Prefix(SpriteBatch spriteBatch) {
			// Draw the Eternia Crystal bar alongside an possible actual boss bar.
			var index = NPC.FindFirstNPC(NPCID.DD2EterniaCrystal);
			if (index >= 0) {
				var npc = Main.npc[index];
				var texture = TextureAssets.NpcHeadBoss[NPCID.Sets.BossHeadTextures[NPCID.DD2EterniaCrystal]].Value;
				var life = Terraria.GameContent.Events.DD2Event.LostThisRun ? 0 : npc.life;
				bossBarExtraOffset = new(0, -50);
				BigProgressBarHelper.DrawFancyBar(spriteBatch, 0, 0, texture, texture.Frame(), life, npc.lifeMax);
			}
		}
	}

	internal class BossBarHelperPatch : Patch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(BigProgressBarHelper), nameof(BigProgressBarHelper.DrawFancyBar), typeof(SpriteBatch), typeof(float), typeof(float), typeof(Texture2D), typeof(Rectangle), typeof(float), typeof(float));

		public override void PatchMethodBody(MethodDef method) {
			// Insert code to add an extra offset after `Rectangle rectangle = Utils.CenteredRectangle(Main.ScreenSize.ToVector2() * new Vector2(0.5f, 1f) + new Vector2(0f, -50f), p.ToVector2());`
			var instructions = method.Body.Instructions;
			for (int i = 1; i < instructions.Count; i++) {
				if (instructions[i].IsStloc()
					&& instructions[i - 1].Is(Code.Call) && ((IMethod) instructions[i - 1].Operand).Name == nameof(Utils.CenteredRectangle)) {
					var local = instructions[i].GetLocal(method.Body.Variables);
					i++;

					instructions.Insert(i, OpCodes.Ldloca_S.ToInstruction(local));
					instructions.Insert(i + 1, Call(EterniaCrystalBossBar.ApplyExtraOffset));
					return;
				}
			}
			throw new ArgumentException("Couldn't find code to replace.");
		}
	}
}
