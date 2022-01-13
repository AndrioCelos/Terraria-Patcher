#nullable enable

using System;

using Microsoft.Xna.Framework;

namespace TerrariaPatcher.Mods;

public static class ModManager {
	public static readonly Color AccentColor = Color.MediumPurple;
	public static readonly Color SuccessColor = new(64, 192, 64);
	public static readonly Color FailColor = new(192, 64, 64);

	public static event EventHandler? Initialising;

	public static void Initialise() => Initialising?.Invoke(null, EventArgs.Empty);
}
