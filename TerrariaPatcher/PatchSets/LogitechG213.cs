#nullable enable

using System;
using System.Runtime.InteropServices;

using dnlib.DotNet.Emit;

using Microsoft.Xna.Framework;

using ReLogic.Peripherals.RGB;

using TerrariaPatcher.Properties;

namespace TerrariaPatcher.PatchSets;

internal class LogitechG213 : PatchSet {
	public override string Name => "Logitech G213 Support";
	public override Version Version => new(1, 1);
	public override string Description => "Modifies the Logitech LED integration to support the G213 keyboard.";
	public override string TargetModuleName => "ReLogic";

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor - it will be set via reflection.
	[ReversePatch(typeof(RgbDevice), "GetProcessedLedColor")]
	public static Func<RgbDevice, int, Vector4> GetProcessedLedColor;
#pragma warning restore CS8618

	public override void BeforeApply()
		=> CopyFileToOutputDirectory(Resources.LogitechLedEnginesWrapper, "LogitechLedEnginesWrapper.dll", true);

	internal class LogitechKeyboardConstructorPatch : Patch {
		public override PatchTarget TargetMethod => PatchTarget.Constructor("ReLogic", "ReLogic.Peripherals.RGB.Logitech.LogitechKeyboard", typeof(DeviceColorProfile));

		public override void PatchMethodBody(dnlib.DotNet.MethodDef method) {
			// Replaces `new Rectangle(0, 0, 21, 6)` with `new Rectangle(0, 0, 5, 1)`.
			var instructions = method.Body.Instructions;
			for (int i = 0; i < instructions.Count; i++) {
				if (instructions[i].IsConstant(21))
					instructions[i] = OpCodes.Ldc_I4_5.ToInstruction();
				else if (instructions[i].IsConstant(6))
					instructions[i] = OpCodes.Ldc_I4_1.ToInstruction();
			}
		}
	}
	
	internal class PresentPatch : PrefixPatch {
		public override PatchTarget TargetMethod
			=> PatchTarget.Create("ReLogic", "ReLogic.Peripherals.RGB.Logitech.LogitechKeyboard", "Present");

		public static bool Prefix(RgbDevice __instance) {
			for (int i = 0; i < 5; ++i) {
				var colour = GetProcessedLedColor(__instance, i);
				LogitechGSDK.LogiLedSetLightingForTargetZone(LogitechGSDK.DeviceType.Keyboard, i + 1,
					(int) (colour.X * 100), (int) (colour.Y * 100), (int) (colour.Z * 100));
			}
			return Program.SKIP_ORIGINAL;
		}
	}

	internal class RenderPatch : PrefixPatch {
		public override PatchTarget TargetMethod
			=> PatchTarget.Create("ReLogic", "ReLogic.Peripherals.RGB.Logitech.LogitechKeyboard", "Render");

		// This keyboard doesn't support per-key lighting, so we do nothing here.
		public static bool Prefix() => Program.SKIP_ORIGINAL;
	}

	internal class SingleLightDevicePresentPatch : PrefixPatch {
		public override PatchTarget TargetMethod
			=> PatchTarget.Create("ReLogic", "ReLogic.Peripherals.RGB.Logitech.LogitechSingleLightDevice", "Present");

		// This would override the keyboard lighting, so out it goes.
		public static bool Prefix() => Program.SKIP_ORIGINAL;
	}

	internal static class LogitechGSDK {
		internal enum DeviceType {
			Keyboard = 0x0,
			Mouse = 0x3,
			Mousemat = 0x4,
			Headset = 0x8,
			Speaker = 0xe
		}

		//LED SDK
		private const int LOGI_DEVICETYPE_MONOCHROME_ORD = 0;
		private const int LOGI_DEVICETYPE_RGB_ORD = 1;
		private const int LOGI_DEVICETYPE_PERKEY_RGB_ORD = 2;

		public const int LOGI_DEVICETYPE_MONOCHROME = (1 << LOGI_DEVICETYPE_MONOCHROME_ORD);
		public const int LOGI_DEVICETYPE_RGB = (1 << LOGI_DEVICETYPE_RGB_ORD);
		public const int LOGI_DEVICETYPE_PERKEY_RGB = (1 << LOGI_DEVICETYPE_PERKEY_RGB_ORD);
		public const int LOGI_DEVICETYPE_ALL = (LOGI_DEVICETYPE_MONOCHROME | LOGI_DEVICETYPE_RGB | LOGI_DEVICETYPE_PERKEY_RGB);

		public const int LOGI_LED_BITMAP_WIDTH = 21;
		public const int LOGI_LED_BITMAP_HEIGHT = 6;
		public const int LOGI_LED_BITMAP_BYTES_PER_KEY = 4;

		public const int LOGI_LED_BITMAP_SIZE = LOGI_LED_BITMAP_WIDTH * LOGI_LED_BITMAP_HEIGHT * LOGI_LED_BITMAP_BYTES_PER_KEY;
		public const int LOGI_LED_DURATION_INFINITE = 0;

		[DllImport("LogitechLedEnginesWrapper", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool LogiLedSetLightingForTargetZone(DeviceType deviceType, int zone, int redPercentage, int greenPercentage, int bluePercentage);
	}
}
