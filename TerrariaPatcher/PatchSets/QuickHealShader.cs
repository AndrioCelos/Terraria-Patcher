#nullable enable

using System;

using Microsoft.Xna.Framework;

using ReLogic.Peripherals.RGB;

using Terraria;
using Terraria.GameContent.RGB;
using Terraria.Initializers;

namespace TerrariaPatcher.PatchSets;

internal class QuickHealShaderMod : PatchSet {
	public override string Name => "Quick Heal RGB Lighting";
	public override Version Version => new(1, 0);
	public override string Description => "Adds an RGB lighting shader to flash the lights when potion sickness wears off.";
	
	public class QuickHealCondition : CommonConditions.ConditionBase {
		public override bool IsActive() => this.CurrentPlayer.potionDelay is > 0 and <= 60;
	}

	public class QuickHealShader : ChromaShader {
		[RgbProcessor(new[] { EffectDetailLevel.Low, EffectDetailLevel.High }, IsTransparent = true)]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Required to conform to a delegate for reflection.")]
		public void ProcessAnyDetail(RgbDevice device, Fragment fragment, EffectDetailLevel quality, float time) {
			var color = (Main.player[Main.myPlayer].potionDelay > 0) ? Vector3.UnitX : Vector3.One;
			fragment.SetColor(0, new Vector4(color, 1f));
			if (fragment.Count > 1)
				fragment.SetColor(1, new Vector4(color, 0.5f));
			for (var i = 2; i < fragment.Count; i++)
				fragment.SetColor(i, Vector4.Zero);
		}
	}

	internal class ChromaInitializerPatch : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(ChromaInitializer.Load);
		public static void Postfix(ChromaEngine ____engine)
			=> ____engine.RegisterShader(new QuickHealShader(), new QuickHealCondition(), ShaderLayer.CriticalAlert);
	}
}
