#nullable enable

using System;
using System.Collections.Generic;

using dnlib.DotNet;
using dnlib.DotNet.Emit;

using Microsoft.Xna.Framework;

using Terraria;
using Terraria.ID;

namespace TerrariaPatcher.PatchSets;

internal class ColouredMetalDetector : PatchSet {
	public override string Name => "Coloured Metal Detector";
	public override Version Version => new(1, 1);
	public override string Description => "Colours the metal detector text depending on the detected treasure.";

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

	private static readonly Dictionary<int, Color> TileColours = new() {
		{ TileID.Pots           , Color.White },
		{ TileID.DesertFossil   , Blue },
		{ TileID.FossilOre      , Blue },
		{ TileID.Copper         , Blue },
		{ TileID.Tin            , Blue },
		{ TileID.Iron           , Blue },
		{ TileID.Lead           , Blue },
		{ TileID.Silver         , Blue },
		{ TileID.Tungsten       , Blue },
		{ TileID.Gold           , Green },
		{ TileID.Platinum       , Green },
		{ TileID.Demonite       , Green },
		{ TileID.Crimtane       , Green },
		{ TileID.Meteorite      , Green },
		{ TileID.Containers     , Orange },
		{ TileID.Containers2    , Orange },
		{ TileID.FakeContainers , Orange },
		{ TileID.FakeContainers2, Orange },
		{ TileID.Cobalt         , LightRed },
		{ TileID.Palladium      , LightRed },
		{ TileID.Mythril        , LightRed },
		{ TileID.Orichalcum     , LightRed },
		{ TileID.Adamantite     , Pink },
		{ TileID.Titanium       , Pink },
		{ TileID.Chlorophyte    , Lime },
		{ TileID.DyePlants      , LightPurple },
		{ TileID.Heart          , LightRed },
		{ TileID.LifeFruit      , Yellow }
	};

	internal static void GetDisplayColour(int tileType, ref Color infoColour, ref Color shadowColour) {
		if (TileColours.TryGetValue(tileType, out var color)) {
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
			var infoTextColourLocal = ColouredInfoAccessoriesHelper.InfoColourLocal;
			var infoTextShadowColourLocal = ColouredInfoAccessoriesHelper.InfoShadowColourLocal;

			// Insert code just before the line setting a bool local to true after `Language.GetTextValue("GameUI.OreDetected", ...)`.
			var instructions = method.Body.Instructions;
			for (var i = 0; i < instructions.Count; i++) {
				if (instructions[i].IsConstant("GameUI.OreDetected")) {
					for (i++; i < instructions.Count; i++) {
						if (instructions[i].IsConstant(1)) {
							var refInstruction = instructions[i];
							var newRefInstruction = new Instruction(OpCodes.Ldloc_S);

							// This instruction is a branch target; find the branch and repoint it at the code we're inserting.
							for (var j = i - 1; j >= 0; j--) {
								if (instructions[j].Operand == refInstruction) {
									instructions[j].Operand = newRefInstruction;

									// Find the second stloc.s after that branch to get the local variable to access.
									for (j += 3; j < i; j++) {
										if (instructions[j].Is(Code.Stloc_S)) {
											var treasureTileLocal = (Local) instructions[j].Operand;
											newRefInstruction.Operand = treasureTileLocal;

											instructions.Insert(i, newRefInstruction);
											instructions.Insert(i + 1, OpCodes.Ldloca_S.ToInstruction(infoTextColourLocal));
											instructions.Insert(i + 2, OpCodes.Ldloca_S.ToInstruction(infoTextShadowColourLocal));
											instructions.Insert(i + 3, Call(ColouredMetalDetector.GetDisplayColour));

											return;
										}
									}
									throw new ArgumentException("Couldn't find local.");
								}
							}
							throw new ArgumentException("Couldn't find branch to replace.");
						}
					}
				}
			}
			throw new ArgumentException("Couldn't find code to replace.");
		}
	}
}
