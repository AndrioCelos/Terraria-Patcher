#nullable enable

using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using ReLogic.Graphics;

using Terraria;
using Terraria.Chat;
using Terraria.Chat.Commands;
using Terraria.Localization;
using Terraria.GameContent;
using Terraria.GameContent.UI.Chat;
using Terraria.UI.Chat;

namespace TerrariaPatcher.PatchSets;

internal class ColouredChatNames : PatchSet {
	public override string Name => "Coloured Chat Names";
	public override Version Version => new(1, 0);
	public override string Description => "Colours player names in chat.";
	public static string GenerateTag(string name, bool prefix)
		=> $"[n{(prefix ? "" : "/b")}:{name.Replace("[", "\\[").Replace("]", "\\]")}]";

	internal class EmoteCommandPatch : PrefixPatch {
		private static readonly Color RESPONSE_COLOR = new(200, 100, 0);

		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(EmoteCommand), nameof(EmoteCommand.ProcessIncomingMessage));

		public static bool Prefix(string text, byte clientId) {
			if (text != "") {
				text = $"*{GenerateTag(Main.player[clientId].name, false)} {text}";
				ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(text), RESPONSE_COLOR, -1);
			}
			return Program.SKIP_ORIGINAL;
		}
	}

	internal class ParsePatch : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(NameTagHandler), "Terraria.UI.Chat.ITagHandler.Parse");

		public static bool Prefix(string text, string options, out TextSnippet __result) {
			__result = new NameSnippet(text.Replace(@"\[", "[").Replace(@"\]", "]"), options != "b");
			return Program.SKIP_ORIGINAL;
		}
	}

	internal class GenerateTagPatch : PrefixPatch {
		public override PatchTarget TargetMethod => PatchTarget.Create(typeof(NameTagHandler), "GenerateTag");

		public static bool Prefix(string name, out string __result) {
			__result = GenerateTag(name, true);
			return Program.SKIP_ORIGINAL;
		}
	}

	public class NameSnippet : TextSnippet {
		public bool IsPrefix { get; }
		public Color NameColour { get; }

		private readonly Vector2 nameSize;
		private readonly Vector2 fullSize;

		public NameSnippet(string text, bool isPrefix) : base(text, Color.White) {
			this.IsPrefix = isPrefix;
			this.NameColour = GetColour(text);
			this.nameSize = FontAssets.MouseText.Value.MeasureString(text);
			this.fullSize = isPrefix ? FontAssets.MouseText.Value.MeasureString(text + ":") : this.nameSize;
		}

		public static Color GetColour(string text) {
			if (text is null) return Color.White;
			var sum = 0;
			for (var i = text.Length - 1; i >= 0 && text[i] is not (')' or ']' or '}'); i--) {
				if (char.IsLetterOrDigit(text[i])) sum += text[i];
			}
			return (sum % 9) switch {
				0 => new Color(  0, 160,   0),
				1 => new Color(255,  32,  32),
				2 => new Color(160,   0, 160),
				3 => new Color(255, 255,  32),
				4 => new Color( 32, 255,  32),
				5 => new Color(  0, 160, 160),
				6 => new Color( 32, 255, 255),
				7 => new Color( 32,  32, 255),
				8 => new Color(255,  32, 255),
				_ => Color.White,
			};
		}

		public override bool UniqueDraw(bool justCheckingString, out Vector2 size, SpriteBatch spriteBatch, Vector2 position = default, Color color = default, float scale = 1) {
			size = this.fullSize;
			if (!justCheckingString) {
				spriteBatch.DrawString(FontAssets.MouseText.Value, this.Text, position, color == Color.Black ? color : this.NameColour);
				if (this.IsPrefix)
					spriteBatch.DrawString(FontAssets.MouseText.Value, ":", new(position.X + this.nameSize.X, position.Y), color);
			}
			return true;
		}
	}
}
