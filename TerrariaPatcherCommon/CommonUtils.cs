#nullable enable

using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

using Microsoft.Win32;

namespace TerrariaPatcherCommon;

public static class CommonUtils {
	public const string DEFAULT_TERRARIA_DIRECTORY = @"C:\Program Files (x86)\Steam\steamapps\common\Terraria";

	public static string? GuiGetTerrariaExePath(string[] args) {
		if (args.Length > 0 && Directory.Exists(args[0]) && File.Exists(Path.Combine(args[0], "Terraria.exe")))
			return Path.Combine(args[0], "Terraria.exe");
		else if (Environment.OSVersion.Platform == PlatformID.Win32NT && GetTerrariaDirectoryFromRegistry() is string path
			&& File.Exists(Path.Combine(path, "Terraria.exe")))
			return Path.Combine(path, "Terraria.exe");
		else if (Directory.Exists(DEFAULT_TERRARIA_DIRECTORY) && File.Exists(Path.Combine(DEFAULT_TERRARIA_DIRECTORY, "Terraria.exe")))
			return Path.Combine(DEFAULT_TERRARIA_DIRECTORY, "Terraria.exe");
		else {
			var openFileDialog = new OpenFileDialog() { Title = "Please locate your Terraria installation", Filter = "Terraria.exe|Terraria.exe" };
			return openFileDialog.ShowDialog() != DialogResult.OK ? null : openFileDialog.FileName;
		}
	}

	private static string? GetTerrariaDirectoryFromRegistry() {
		using var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
		using var subKey = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 105600");
		return subKey?.GetValue("InstallLocation") as string;
	}

	public static void ExtractResource(string exePath, string resourceName, string outputPath) {
		if (!File.Exists(outputPath)) {
			var assembly = Assembly.LoadFrom(exePath);
			var inputStream = assembly.GetManifestResourceStream(resourceName) ?? throw new ArgumentException($"{resourceName} not found");
			using var outputStream = File.OpenWrite(outputPath);
			var bytes = new byte[4096];
			while (true) {
				var n = inputStream.Read(bytes, 0, bytes.Length);
				if (n == 0) break;
				outputStream.Write(bytes, 0, n);
			}
		}
	}
}
