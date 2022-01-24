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

		var outputDir = Path.Combine("..", "..", "..", "..", "refs");
		if (!Directory.Exists(outputDir)) outputDir = ".";

		try {
			var exePath = Utils.GuiGetTerrariaExePath(args);
			var exeDir = Path.GetDirectoryName(exePath);
			if (exePath is null) return 1;

			if (!File.Exists(Path.Combine(outputDir, "Terraria.exe")))
				File.Copy(exePath, Path.Combine(outputDir, "Terraria.exe"));
			if (File.Exists(Path.Combine(outputDir, "ReLogic.dll")))
				MessageBox.Show("ReLogic.dll already exists. Build the patcher now.", "Terraria Patcher", MessageBoxButtons.OK, MessageBoxIcon.Information);
			else {
				Utils.ExtractResource(exePath, "Terraria.Libraries.ReLogic.ReLogic.dll", Path.Combine(outputDir, "ReLogic.dll"));
				MessageBox.Show("Successfully extracted ReLogic.dll. Build the patcher now.", "Terraria Patcher", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			return 0;
		} catch (Exception ex) {
			MessageBox.Show($"An exception occurred:\n{ex}", "Terraria Patcher", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return 1;
		}
	}
}
