#nullable enable

using System;
using System.Text;

using Microsoft.Xna.Framework.Input;

namespace TerrariaPatcher.Mods;

public class Keystroke {
	public Keys[] Keys { get; }
	public ModifierKeys Modifiers { get; }

	public Keystroke(Keys[] keys, ModifierKeys modifiers) {
		Array.Sort(keys ?? throw new ArgumentNullException(nameof(keys)), 0, keys.Length - 1);
		this.Keys = keys;
		this.Modifiers = modifiers;
	}

	public override bool Equals(object other) {
		if (other is not Keystroke keystroke) return false;
		if (this.Keys.Length != keystroke.Keys.Length) return false;
		if (this.Modifiers != keystroke.Modifiers) return false;
		for (var i = this.Keys.Length - 1; i >= 0; --i) {
			if (this.Keys[i] != keystroke.Keys[i]) return false;
		}
		return true;
	}
	public override int GetHashCode() {
		var hashCode = new HashCode();
		hashCode.Add(this.Modifiers);
		foreach (var key in this.Keys)
			hashCode.Add(key);
		return hashCode.ToHashCode();
	}

	public override string ToString() {
		var builder = new StringBuilder();
		if (this.Modifiers.HasFlag(ModifierKeys.Shift)) builder.Append("Shift+");
		if (this.Modifiers.HasFlag(ModifierKeys.Control)) builder.Append("Ctrl+");
		if (this.Modifiers.HasFlag(ModifierKeys.Alt)) builder.Append("Alt+");
		if (this.Modifiers.HasFlag(ModifierKeys.Windows)) builder.Append("Win+");

		if (this.Keys.Length == 0)
			builder.Remove(builder.Length - 1, 1);
		else {
			builder.Append(this.Keys[0]);
			for (var i = 1; i < this.Keys.Length; ++i) {
				builder.Append('+');
				builder.Append(this.Keys[i]);
			}
		}
		return builder.ToString();
	}

	[Flags]
	public enum ModifierKeys {
		Control = 1,
		Shift = 2,
		Alt = 4,
		Windows = 8
	}
}
