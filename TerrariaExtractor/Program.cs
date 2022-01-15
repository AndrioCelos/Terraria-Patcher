#nullable enable

using System;
using System.IO;
using System.Windows.Forms;

using TerrariaPatcher;

namespace TerrariaExtractor;

internal static class Program {
	/// <summary>
	/// The main entry point for the application.
	/// </summary>
	[STAThread]
	static int Main(string[] args) {
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(false);

		try {
			var directory = Utils.GuiGetTerrariaDirectory(args);
			if (directory is null) return 1;

			if (File.Exists(Path.Combine(directory, "ReLogic.dll")))
				MessageBox.Show("ReLogic.dll already exists. Build the patcher now.", "Terraria Patcher", MessageBoxButtons.OK, MessageBoxIcon.Information);
			else {
				Utils.ExtractResource(directory, "Terraria.Libraries.ReLogic.ReLogic.dll", "ReLogic.dll");
				MessageBox.Show("Successfully extracted ReLogic.dll. Build the patcher now.", "Terraria Patcher", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			return 0;
		} catch (Exception ex) {
			MessageBox.Show($"An exception occurred:\n{ex}", "Terraria Patcher", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return 1;
		}
	}
}
