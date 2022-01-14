#nullable enable

using System;
using System.Collections.Generic;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

using Microsoft.Xna.Framework;

using Terraria;
using Terraria.Audio;
using Terraria.ID;

using TerrariaPatcher.Mods;

namespace TerrariaPatcher.PatchSets;

internal class ColouredLifeformAnalyzer : PatchSet {
	public override string Name => "Coloured Lifeform Analyzer";
	public override Version Version => new(1, 0);
	public override string Description => "Colours the lifeform analyzer text depending on the detected lifeform.";
	public override IReadOnlyCollection<Type> Dependencies => new[] { typeof(ColouredInfoAccessories) };

	private static Color Gray => new(130, 130, 130);
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

	private static readonly Dictionary<int, Color> NpcColours = new() {
		{ NPCID.Pinky               , Pink },
		{ NPCID.UndeadMiner         , Blue },
		{ NPCID.Tim                 , Green },
		{ NPCID.DoctorBones         , Green },
		{ NPCID.TheGroom            , Blue },
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
		{ NPCID.GoldenSlime         , Orange },
		{ NPCID.TruffleWorm         , LightPurple },
		{ NPCID.GoldBird            , Orange },
		{ NPCID.GoldBunny           , Orange },
		{ NPCID.GoldButterfly       , Orange },
		{ NPCID.GoldFrog            , Orange },
		{ NPCID.GoldGrasshopper     , Orange },
		{ NPCID.GoldMouse           , Orange },
		{ NPCID.GoldWorm            , Orange },
		{ NPCID.SquirrelGold        , Orange },
		{ NPCID.FairyCritterPink    , Pink },
		{ NPCID.FairyCritterGreen   , Green },
		{ NPCID.FairyCritterBlue    , Blue },
		{ NPCID.GoldGoldfish        , Orange },
		{ NPCID.GoldGoldfishWalker  , Orange },
		{ NPCID.GoldDragonfly       , Orange },
		{ NPCID.GoldLadyBug         , Orange },
		{ NPCID.GoldWaterStrider    , Orange },
		{ NPCID.GoldSeahorse        , Orange },
		{ NPCID.EmpressButterfly    , LightPurple },
		{ NPCID.BoundGoblin         , Green },
		{ NPCID.BoundWizard         , LightPurple },
		{ NPCID.BoundMechanic       , Orange },
		{ NPCID.WebbedStylist       , Green },
		{ NPCID.SleepingAngler      , Green },
		{ NPCID.SkeletonMerchant    , Green },
		{ NPCID.BartenderUnconscious, Green },
		{ NPCID.GolferRescue        , Green }
	};

	internal static void GetDisplayColour(int npcIndex, ref Color infoColour) {
		var npc = npcIndex >= 0 && npcIndex < Main.npc.Length ? Main.npc[npcIndex] : null;
		var color = npc is null || !npc.active ? Gray
			: NpcColours.TryGetValue(npc.type, out var color1) ? color1
			: Color.White;
		infoColour.R = (byte) (infoColour.R * color.R / 255);
		infoColour.G = (byte) (infoColour.G * color.G / 255);
		infoColour.B = (byte) (infoColour.B * color.B / 255);
		infoColour.A = (byte) (infoColour.A * color.A / 255);
	}

	internal class DrawInfoAccsPatch : Patch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(Main), "DrawInfoAccs");

		public override void PatchMethodBody(MethodDef method) {
			// Replace the code after `text3 = Lang.inter[105].Value` that builds the display string.
			var instructions = method.Body.Instructions;
			for (int i = 3; i < instructions.Count; i++) {
				if (instructions[i - 3].IsConstant(105)
					&& instructions[i].IsStloc()) {

					for (i += 3; i < instructions.Count; i++) {
						if (instructions[i - 2].Is(Code.Ldloc_S)
							&& instructions[i - 1].IsConstant(0)
							&& instructions[i].Is(Code.Blt_S)) {

							var refInstruction = instructions[i - 2];
							var newRefInstruction = new Instruction(OpCodes.Ldloc_S, instructions[i - 2].Operand);
							
							var foundBranch = false;
							for (var j = i - 3; j >= 0; j--) {
								if (instructions[j].Operand == refInstruction) {
									foundBranch = true;
									instructions[j].Operand = newRefInstruction;
									break;
								}
							}
							if (!foundBranch) throw new ArgumentException("Couldn't find branch to replace.");

							instructions.Insert(i - 2, newRefInstruction);
							instructions.Insert(i - 1, OpCodes.Ldloca_S.ToInstruction(ColouredInfoAccessories.InfoColourLocal));
							instructions.Insert(i, Call(ColouredLifeformAnalyzer.GetDisplayColour));

							return;
						}
					}
				}
			}
			throw new ArgumentException("Couldn't find code to replace.");
		}
	}
}
