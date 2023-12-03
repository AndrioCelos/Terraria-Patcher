#nullable enable

using System;
using System.Collections.Generic;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

using Microsoft.Xna.Framework;

using Terraria;
using Terraria.ID;

namespace TerrariaPatcher.PatchSets;

internal class ColouredLifeformAnalyzer : PatchSet {
	public override string Name => "Coloured Lifeform Analyzer";
	public override Version Version => new(1, 2);
	public override string Description => "Colours the lifeform analyzer text depending on the detected lifeform.";

	private static Color Gray => new(100, 100, 100);
	private static Color Blue => new(150, 150, 255);
	private static Color Green => new(150, 255, 150);
	private static Color Orange => new(255, 200, 150);
	private static Color LightRed => new(255, 150, 150);
	private static Color Pink => new(255, 150, 255);
	private static Color LightPurple => new(210, 160, 255);
	private static Color Lime => new(150, 255, 10);
	private static Color Yellow => new(255, 255, 10);
	private static Color Cyan => new(5, 200, 255);
	private static Color Red => new(255, 40, 100);
	private static Color Purple => new(180, 40, 255);
	private static Color QuestItemColour => new(255, 175, 0);
	private static Color Gold => new(255, 231, 69);

	private static readonly Dictionary<int, Color> NpcColours = new() {
		{ NPCID.Pinky               , Pink },
		{ NPCID.UndeadMiner         , Blue },
		{ NPCID.Tim                 , Green },
		{ NPCID.DoctorBones         , Green },
		{ NPCID.TheGroom            , Blue },
		{ NPCID.VoodooDemon         , Orange },
		{ NPCID.DungeonSlime        , Green },
		{ NPCID.GoblinScout         , Blue },
		{ NPCID.Mimic               , LightRed },
		{ NPCID.Clown               , Pink },
		{ NPCID.RuneWizard          , LightPurple },
		{ NPCID.LostGirl            , Blue },
		{ NPCID.Nymph               , Blue },
		{ NPCID.Moth                , Pink },
		{ NPCID.PirateCaptain       , LightRed },
		//{ NPCID.CochinealBeetle     , Color.White },
		//{ NPCID.CyanBeetle          , Color.White },
		//{ NPCID.LacBeetle           , Color.White },
		//{ NPCID.SeaSnail            , Color.White },
		//{ NPCID.Squid               , Color.White },
		{ NPCID.IceGolem            , Pink },
		{ NPCID.RainbowSlime        , LightRed },
		{ NPCID.Eyezor              , Pink },
		{ NPCID.BoneLee             , Yellow },
		{ NPCID.Paladin             , Yellow },
		{ NPCID.SkeletonSniper      , Yellow },
		{ NPCID.TacticalSkeleton    , Yellow },
		{ NPCID.SkeletonCommando    , Yellow },
		{ NPCID.MartianProbe        , Cyan },
		{ NPCID.GoblinSummoner      , LightRed },
		{ NPCID.BigMimicCorruption  , LightPurple },
		{ NPCID.BigMimicCrimson     , LightPurple },
		{ NPCID.BigMimicHallow      , LightPurple },
		{ NPCID.BigMimicJungle      , LightPurple },
		{ NPCID.Mothron             , Lime },
		{ NPCID.Medusa              , LightRed },
		{ NPCID.DemonTaxCollector   , LightPurple },
		{ NPCID.TheBride            , Blue },
		{ NPCID.SandElemental       , Pink },
		{ NPCID.ZombieMerman        , Orange },
		{ NPCID.EyeballFlyingFish   , Orange },
		{ NPCID.BloodNautilus       , LightRed },
		{ NPCID.GoblinShark         , LightRed },
		{ NPCID.BloodEelHead        , LightRed },
		{ NPCID.Gnome               , Blue },
		{ NPCID.IceMimic            , LightRed },
		{ NPCID.GoldenSlime         , Gold },
		{ NPCID.BoundTownSlimePurple, LightPurple },
		{ NPCID.TruffleWorm         , LightPurple },
		{ NPCID.GoldBird            , Gold },
		{ NPCID.GoldBunny           , Gold },
		{ NPCID.GoldButterfly       , Gold },
		{ NPCID.GoldFrog            , Gold },
		{ NPCID.GoldGrasshopper     , Gold },
		{ NPCID.GoldMouse           , Gold },
		{ NPCID.GoldWorm            , Gold },
		{ NPCID.SquirrelGold        , Gold },
		{ NPCID.FairyCritterPink    , Pink },
		{ NPCID.FairyCritterGreen   , Green },
		{ NPCID.FairyCritterBlue    , Blue },
		{ NPCID.GoldGoldfish        , Gold },
		{ NPCID.GoldGoldfishWalker  , Gold },
		{ NPCID.GoldDragonfly       , Gold },
		{ NPCID.GoldLadyBug         , Gold },
		{ NPCID.GoldWaterStrider    , Gold },
		{ NPCID.GoldSeahorse        , Gold },
		{ NPCID.EmpressButterfly    , LightPurple },
		{ NPCID.BoundTownSlimeYellow, Orange },
		{ NPCID.BoundGoblin         , Green },
		{ NPCID.BoundWizard         , LightPurple },
		{ NPCID.BoundMechanic       , Orange },
		{ NPCID.WebbedStylist       , Green },
		{ NPCID.SleepingAngler      , Green },
		{ NPCID.SkeletonMerchant    , Green },
		{ NPCID.BartenderUnconscious, Green },
		{ NPCID.GolferRescue        , Green },
		{ NPCID.BoundTownSlimeOld   , Orange }
	};

	internal static void GetDisplayColour(int npcIndex, ref Color infoColour, ref Color shadowColour) {
		var npc = npcIndex >= 0 && npcIndex < Main.npc.Length ? Main.npc[npcIndex] : null;
		if (npc != null && npc.active) {
			var color = NpcColours.TryGetValue(npc.netID, out var color1) ? color1 : Color.White;
			infoColour.R = (byte) (infoColour.R * color.R / 255);
			infoColour.G = (byte) (infoColour.G * color.G / 255);
			infoColour.B = (byte) (infoColour.B * color.B / 255);
			infoColour.A = (byte) (infoColour.A * color.A / 255);
			shadowColour = infoColour * 0.1f;
			shadowColour.A = 255;
		}
	}

	internal class DrawInfoAccsPatch : Patch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(Main), "DrawInfoAccs");

		public override void PatchMethodBody(MethodDef method) {
			// Insert code after the call to DrawInfoAccs_AdjustInfoTextColorsForNPC.
			var instructions = method.Body.Instructions;
			for (int i = 4; i < instructions.Count; i++) {
				if (instructions[i - 4].IsLdloc() && instructions[i - 4].Operand is Local local0
					&& instructions[i - 3].Is(Code.Ldelem_Ref)
					&& instructions[i - 2].Is(Code.Ldloca_S) && instructions[i - 2].Operand is Local local1
					&& instructions[i - 1].Is(Code.Ldloca_S) && instructions[i - 1].Operand is Local local2
					&& instructions[i].Is(Code.Call) && ((IMethod) instructions[i].Operand).Name == "DrawInfoAccs_AdjustInfoTextColorsForNPC") {
					instructions.Insert(i + 1, OpCodes.Ldloc_S.ToInstruction(local0));
					instructions.Insert(i + 2, OpCodes.Ldloca_S.ToInstruction(local1));
					instructions.Insert(i + 3, OpCodes.Ldloca_S.ToInstruction(local2));
					instructions.Insert(i + 4, Call(ColouredLifeformAnalyzer.GetDisplayColour));
					return;
				}
			}
			throw new ArgumentException("Couldn't find code to replace.");
		}
	}
}
