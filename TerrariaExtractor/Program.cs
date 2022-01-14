#nullable enable

using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

using Microsoft.Win32;

namespace TerrariaExtractor;

internal static class Program {
	/// <summary>
	/// The main entry point for the application.
	/// </summary>
	[STAThread]
	static int Main(string[] args) {
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(false);

		string directory;
		if (args.Length > 0 && Directory.Exists(args[0]))
			directory = args[0];
		else if (Environment.OSVersion.Platform == PlatformID.Win32NT && GetTerrariaDirectoryFromRegistry() is string path)
			directory = path;
		else if (Directory.Exists(@"C:\Program Files (x86)\Steam\steamapps\common\Terraria"))
			directory = @"C:\Program Files (x86)\Steam\steamapps\common\Terraria";
		else {
			var openFileDialog = new OpenFileDialog() { Title = "Please locate your Terraria installation", Filter = "Terraria.exe|Terraria.exe" };
			if (openFileDialog.ShowDialog() != DialogResult.OK)
				return 1;
			directory = Path.GetDirectoryName(openFileDialog.FileName)!;
		}

		// Extract ReLogic.dll.
		if (!File.Exists(Path.Combine(directory, "ReLogic.dll"))) {
			var assembly = Assembly.LoadFrom(Path.Combine(directory, "Terraria.exe"));
			var inputStream = assembly.GetManifestResourceStream("Terraria.Libraries.ReLogic.ReLogic.dll") ?? throw new Exception("ReLogic.dll not found");
			using var outputStream = File.OpenWrite(Path.Combine(directory, "ReLogic.dll"));
			var bytes = new byte[4096];
			while (true) {
				var n = inputStream.Read(bytes, 0, bytes.Length);
				if (n == 0) break;
				outputStream.Write(bytes, 0, n);
			}
		}

		return 0;
	}

	private static string? GetTerrariaDirectoryFromRegistry() {
		using var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
		using var subKey = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 105600");
		return subKey?.GetValue("InstallLocation") as string;
	}
}
