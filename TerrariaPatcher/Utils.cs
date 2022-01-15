#nullable enable

using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

using Microsoft.Win32;

namespace TerrariaPatcher;

public static class Utils {
	public const string DEFAULT_TERRARIA_DIRECTORY = @"C:\Program Files (x86)\Steam\steamapps\common\Terraria";

	public static string? GuiGetTerrariaDirectory(string[] args) {
		if (args.Length > 0 && Directory.Exists(args[0]))
			return args[0];
		else if (Environment.OSVersion.Platform == PlatformID.Win32NT && GetTerrariaDirectoryFromRegistry() is string path)
			return path;
		else if (Directory.Exists(DEFAULT_TERRARIA_DIRECTORY))
			return DEFAULT_TERRARIA_DIRECTORY;
		else {
			var openFileDialog = new OpenFileDialog() { Title = "Please locate your Terraria installation", Filter = "Terraria.exe|Terraria.exe" };
			return openFileDialog.ShowDialog() != DialogResult.OK ? null : Path.GetDirectoryName(openFileDialog.FileName);
		}
	}

	private static string? GetTerrariaDirectoryFromRegistry() {
		using var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
		using var subKey = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 105600");
		return subKey?.GetValue("InstallLocation") as string;
	}

	public static void ExtractResource(string directory, string resourceName, string outputFileName) {
		var outputFile = Path.Combine(directory, outputFileName);
		if (!File.Exists(outputFile)) {
			var assembly = Assembly.LoadFrom(Path.Combine(directory, "Terraria.exe"));
			var inputStream = assembly.GetManifestResourceStream(resourceName) ?? throw new ArgumentException($"{outputFileName} not found");
			using var outputStream = File.OpenWrite(outputFileName);
			var bytes = new byte[4096];
			while (true) {
				var n = inputStream.Read(bytes, 0, bytes.Length);
				if (n == 0) break;
				outputStream.Write(bytes, 0, n);
			}
		}
	}
}
