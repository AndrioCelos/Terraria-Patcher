#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Terraria;
using Terraria.Audio;
using Terraria.Chat;
using Terraria.UI.Chat;

namespace TerrariaPatcher.Mods;

public delegate void CommandAction(string[] args);

internal static class CommandManager {
	public static List<KeyBinding> KeyBindings { get; } = new();
	public static SortedDictionary<string, CommandAction> Commands { get; } = new(StringComparer.CurrentCultureIgnoreCase);

	public static event EventHandler? Initialising;

	private static List<Keys> currentKeys = new();
	private static readonly List<Keys> currentKeys2 = new();
	private static List<Keys> prevCurrentKeys = new();
	private static Keystroke.ModifierKeys currentModifiers;
	private static float prevMusicVolume;

	static CommandManager() {
		Commands.Add("help", CommandHelp);
		Commands.Add("music", CommandMusic);
		Commands.Add("say", CommandSay);
		Commands.Add("party", CommandParty);
		Commands.Add("pvp", CommandPvP);
		Commands.Add("bind", CommandBind);
		Commands.Add("unbind", CommandUnbind);
		Commands.Add("listbindings", CommandListBindings);
	}

	public static void Message(string s) => Message(s, ModManager.AccentColor);
	public static void Message(string s, Color color) => Main.NewText(s, color.R, color.G, color.B);
	public static void SuccessMessage(string s) => Message(s, ModManager.SuccessColor);
	public static void FailMessage(string s) => Message(s, ModManager.FailColor);

	public static void Initialise() {
		Initialising?.Invoke(null, EventArgs.Empty);
		Load();
	}

	public static void Save() {
		using var fileStream = new FileStream(Path.Combine(Main.SavePath, "commands.dat"), FileMode.Create);
		using var binaryWriter = new BinaryWriter(fileStream);
		binaryWriter.Write(KeyBindings.Count);
		foreach (var binding in KeyBindings) {
			binaryWriter.Write(binding.Keystrokes.Length);
			foreach (var keystroke in binding.Keystrokes) {
				binaryWriter.Write(keystroke.Keys.Length);
				foreach (var key in keystroke.Keys) {
					binaryWriter.Write((int) key);
				}
				binaryWriter.Write((byte) keystroke.Modifiers);
			}
			binaryWriter.Write(binding.Command);
			binaryWriter.Write(binding.Arguments.Length);
			foreach (var parameter in binding.Arguments) {
				binaryWriter.Write(parameter);
			}
		}
		binaryWriter.Close();
	}

	public static void Load() {
		if (File.Exists(Path.Combine(Main.SavePath, "commands.dat"))) {
			using var reader = new BinaryReader(File.Open(Path.Combine(Main.SavePath, "commands.dat"), FileMode.Open, FileAccess.Read));
			var bindingCount = reader.ReadInt32();
			for (var i = 0; i < bindingCount; ++i) {
				var keystrokeCount = reader.ReadInt32();
				var strokes = new Keystroke[keystrokeCount];
				for (var j = 0; j < keystrokeCount; ++j) {
					var keyCount = reader.ReadInt32();
					var keys = new Keys[keyCount];
					for (var k = 0; k < keyCount; ++k)
						keys[k] = (Keys) reader.ReadInt32();

					var modifiers = (Keystroke.ModifierKeys) reader.ReadByte();
					strokes[j] = new(keys, modifiers);
				}

				var commandText = reader.ReadString();
				Commands.TryGetValue(commandText, out var action);

				var parameterCount = reader.ReadInt32();
				var parameters = new string[parameterCount];
				for (var j = 0; j < parameterCount; ++j)
					parameters[j] = reader.ReadString();

				KeyBindings.Add(new KeyBinding(strokes, commandText, action, parameters));
			}
		}
	}

	public static void HandleCommand(string line) {
		string? commandName = null; var args = new List<string>();
		var builder = new StringBuilder();
		for (var i = 0; i < line.Length; i++) {
			for (; i < line.Length; i++) {
				var c = line[i];

				if (c == '"') {
					for (i++; i < line.Length; i++) {
						c = line[i];
						if (c == '"') break;
						builder.Append(char.ToLower(c));
					}
				} else if (char.IsWhiteSpace(c)) break;
				else builder.Append(c);
			}
			if (builder.Length == 0) continue;
			if (commandName is null) commandName = builder.ToString();
			else args.Add(builder.ToString());
			builder.Clear();
		}

		if (commandName is null) return;
		if (Commands.TryGetValue(commandName, out var command)) {
			try {
				command(args.ToArray());
			} catch (Exception ex) {
				FailMessage($"The command failed: {ex}");
			}
		} else
			FailMessage($"Unknown command: {commandName}");
	}

	public static void HandleInput(IEnumerable<Keys> pressedKeys) {
		var keystrokeUpdated = false;

		currentKeys.Clear();
		currentModifiers = 0;
		foreach (var key in pressedKeys) {
			if (prevCurrentKeys.Contains(key)) {
				// Modifiers doesn't include newly-pressed keys.
				switch (key) {
					case Keys.LeftControl: case Keys.RightControl: currentModifiers |= Keystroke.ModifierKeys.Control; break;
					case Keys.LeftShift: case Keys.RightShift: currentModifiers |= Keystroke.ModifierKeys.Shift; break;
					case Keys.LeftAlt: case Keys.RightAlt: currentModifiers |= Keystroke.ModifierKeys.Alt; break;
					case Keys.LeftWindows: case Keys.RightWindows: currentModifiers |= Keystroke.ModifierKeys.Alt; break;
				}
			} else {
				keystrokeUpdated = true;
			}
			currentKeys.Add(key);
		}

		if (keystrokeUpdated) {
			currentKeys2.Clear();
			currentKeys2.AddRange(currentKeys.Where((k, i) => i >= currentKeys.Count - 1 || k is not
				(Keys.LeftControl or Keys.RightControl or Keys.LeftShift or Keys.RightShift
					or Keys.LeftAlt or Keys.RightAlt or Keys.LeftWindows or Keys.RightWindows)));
			currentKeys2.Sort(0, currentKeys2.Count - 1, null);

			var skipFirstKeystrokeBindings = false;
			foreach (var binding in KeyBindings.Where(b => b.progress > 0)) {
				if (binding.Keystrokes[binding.progress].Modifiers == currentModifiers &&
					binding.Keystrokes[binding.progress].Keys.SequenceEqual(currentKeys2)) {
					skipFirstKeystrokeBindings = true;
					binding.progress++;
					if (binding.progress == binding.Keystrokes.Length) {
						binding.Action?.Invoke(binding.Arguments);
						binding.progress = 0;
					}
				} else
					binding.progress = 0;
			}

			if (!skipFirstKeystrokeBindings) {
				foreach (var binding in KeyBindings.Where(b => b.progress == 0)) {
					if (binding.Keystrokes[0].Modifiers == currentModifiers &&
						binding.Keystrokes[0].Keys.SequenceEqual(currentKeys2)) {
						binding.progress++;
						if (binding.progress == binding.Keystrokes.Length) {
							binding.Action?.Invoke(binding.Arguments);
							binding.progress = 0;
						}
					} else
						binding.progress = 0;
				}
			}
		}

		var swap = currentKeys;
		currentKeys = prevCurrentKeys;
		prevCurrentKeys = swap;
		currentKeys.Clear();
	}

	private static void CommandMusic(string[] parameters) {
		if (parameters.Length != 1) {
			FailMessage("Usage: .music on|off|toggle|[+|-]<volume>");
			return;
		}
		float offset;
		if (parameters[0].Equals("on", StringComparison.OrdinalIgnoreCase)) {
			if (prevMusicVolume > 0) {
				Main.musicVolume = prevMusicVolume;
				prevMusicVolume = 0;
			}
		} else if (parameters[0].Equals("off", StringComparison.OrdinalIgnoreCase)) {
			if (Main.musicVolume > 0) {
				prevMusicVolume = Main.musicVolume;
				Main.musicVolume = 0;
			}
		} else if (parameters[0].Equals("toggle", StringComparison.OrdinalIgnoreCase)) {
			if (Main.musicVolume == 0) {
				Main.musicVolume = prevMusicVolume;
				prevMusicVolume = 0;
			} else {
				prevMusicVolume = Main.musicVolume;
				Main.musicVolume = 0;
			}
		} else if (parameters[0].StartsWith("+") || parameters[0].StartsWith("-")) {
			if (float.TryParse(parameters[0].Substring(1), out offset)) {
				if (float.IsNaN(offset)) {
					FailMessage("Usage: .music on|off|toggle|[+|-]<volume>");
					return;
				}
				offset /= parameters[0][0] == '-' ? -100 : 100;
				Main.musicVolume += offset;
				if (Main.musicVolume > 1f) Main.musicVolume = 1f;
				else if (Main.musicVolume < 0f) Main.musicVolume = 0f;
			} else {
				FailMessage("Usage: .music on|off|toggle|[+|-]<volume>");
			}
		} else if (float.TryParse(parameters[0], out offset)) {
			if (float.IsNaN(offset)) {
				FailMessage("Usage: .music on|off|toggle|[+|-]<volume>");
				return;
			}
			Main.musicVolume = offset / 100f;
			if (Main.musicVolume > 1f) Main.musicVolume = 1f;
			else if (Main.musicVolume < 0f) Main.musicVolume = 0f;
		} else {
			FailMessage("Usage: .music on|off|toggle|[+|-]<volume>");
		}
	}

	private static void CommandSay(string[] parameters) {
		var message = ChatManager.Commands.CreateOutgoingMessage(string.Join(" ", parameters));
		if (Main.netMode == 1) {
			ChatHelper.SendChatMessageFromClient(message);
			return;
		} else
			ChatManager.Commands.ProcessIncomingMessage(message, Main.myPlayer);
	}

	public static Keystroke[] ParseKeystrokes(string text) {
		var keystrokeStrings = text.Split(new char[] { ',' });
		var keystrokes = new Keystroke[keystrokeStrings.Length];

		for (var i = 0; i < keystrokes.Length; i++) {
			var keyStrings = keystrokeStrings[i].Split(new char[] { '+' });
			var keys = new List<Keys>();
			Keystroke.ModifierKeys modifiers = 0;
			for (var j = 0; j < keyStrings.Count(); j++) {
				var thisKey = keyStrings[j];
				if (thisKey.Length == 1 && thisKey[0] is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z')) {
					keys.Add((Keys) char.ToUpperInvariant(thisKey[0]));
				} else if (thisKey.Equals("ctrl", StringComparison.CurrentCultureIgnoreCase) || thisKey.Equals("control", StringComparison.CurrentCultureIgnoreCase)) {
					modifiers |= Keystroke.ModifierKeys.Control;
				} else if (thisKey.Equals("shift", StringComparison.CurrentCultureIgnoreCase)) {
					modifiers |= Keystroke.ModifierKeys.Shift;
				} else if (thisKey.Equals("alt", StringComparison.CurrentCultureIgnoreCase)) {
					modifiers |= Keystroke.ModifierKeys.Alt;
				} else if (thisKey.Equals("windows", StringComparison.CurrentCultureIgnoreCase) || thisKey.Equals("windows", StringComparison.CurrentCultureIgnoreCase)
					 || thisKey.Equals("super", StringComparison.CurrentCultureIgnoreCase)) {
					modifiers |= Keystroke.ModifierKeys.Windows;
				} else {
					var (key2, modifiers2) = ParseKey(thisKey);
					keys.Add(key2);
					modifiers |= modifiers2;
				}
			}

			keystrokes[i] = new(keys.ToArray(), modifiers);
		}
		return keystrokes;
	}

	private static (Keys key, Keystroke.ModifierKeys modifiers) ParseKey(string s) => s.ToLower() switch {
		"0" => (Keys.D0, 0),
		"1" => (Keys.D1, 0),
		"2" => (Keys.D2, 0),
		"3" => (Keys.D3, 0),
		"4" => (Keys.D4, 0),
		"5" => (Keys.D5, 0),
		"6" => (Keys.D6, 0),
		"7" => (Keys.D7, 0),
		"8" => (Keys.D8, 0),
		"9" => (Keys.D9, 0),

		")" => (Keys.D0, Keystroke.ModifierKeys.Shift),
		"!" => (Keys.D1, Keystroke.ModifierKeys.Shift),
		"@" => (Keys.D2, Keystroke.ModifierKeys.Shift),
		"#" => (Keys.D3, Keystroke.ModifierKeys.Shift),
		"$" => (Keys.D4, Keystroke.ModifierKeys.Shift),
		"%" => (Keys.D5, Keystroke.ModifierKeys.Shift),
		"^" => (Keys.D6, Keystroke.ModifierKeys.Shift),
		"&" => (Keys.D7, Keystroke.ModifierKeys.Shift),
		"*" => (Keys.D8, Keystroke.ModifierKeys.Shift),
		"(" => (Keys.D9, Keystroke.ModifierKeys.Shift),

		"`" or "backtick" or "grave" => (Keys.OemTilde, 0),
		"~" or "tilde" => (Keys.OemTilde, Keystroke.ModifierKeys.Shift),
		"-" or "dash" or "hyphen" => (Keys.OemMinus, 0),
		"_" or "underscore" => (Keys.OemMinus, Keystroke.ModifierKeys.Shift),
		"=" or "equals" => (Keys.OemPlus, 0),
		"+" or "plus" => (Keys.OemPlus, Keystroke.ModifierKeys.Shift),
		"[" => (Keys.OemOpenBrackets, 0),
		"{" => (Keys.OemOpenBrackets, Keystroke.ModifierKeys.Shift),
		"]" => (Keys.OemCloseBrackets, 0),
		"}" => (Keys.OemCloseBrackets, Keystroke.ModifierKeys.Shift),
		"\\" or "backslash" => (Keys.OemBackslash, 0),
		"|" or "pipe" => (Keys.OemBackslash, Keystroke.ModifierKeys.Shift),
		";" or "semicolon" => (Keys.OemSemicolon, 0),
		":" or "colon" => (Keys.OemSemicolon, Keystroke.ModifierKeys.Shift),
		"'" or "quote" => (Keys.OemQuotes, 0),
		"\"" => (Keys.OemQuotes, Keystroke.ModifierKeys.Shift),
		"," or "comma" => (Keys.OemComma, 0),
		"<" => (Keys.OemComma, Keystroke.ModifierKeys.Shift),
		"." or "period" or "dot" => (Keys.OemPeriod, 0),
		">" => (Keys.OemPeriod, Keystroke.ModifierKeys.Shift),
		"/" or "slash" => (Keys.OemQuestion, 0),
		"?" => (Keys.OemQuestion, Keystroke.ModifierKeys.Shift),

		"esc" or "escape" => (Keys.Escape, 0),
		"printscreen" or "print_screen" or "print-screen" or "prtsc" => (Keys.PrintScreen, 0),
		"pause" or "break" => (Keys.Pause, 0),
		"back" or "backspace" or "bksp" => (Keys.Back, 0),
		"caps" or "capslock" or "caps-lock" or "caps_lock" => (Keys.CapsLock, 0),
		"enter" or "return" => (Keys.Enter, 0),
		"insert" or "ins" => (Keys.Insert, 0),
		"delete" or "del" => (Keys.Delete, 0),
		"pageup" or "page_up" or "page-up" or "pgup" => (Keys.PageUp, 0),
		"pagedown" or "page_down" or "page-down" or "pgdn" => (Keys.PageDown, 0),

		"numpad0" or "num0" => (Keys.NumPad0, 0),
		"numpad1" or "num1" => (Keys.NumPad1, 0),
		"numpad2" or "num2" => (Keys.NumPad2, 0),
		"numpad3" or "num3" => (Keys.NumPad3, 0),
		"numpad4" or "num4" => (Keys.NumPad4, 0),
		"numpad5" or "num5" => (Keys.NumPad5, 0),
		"numpad6" or "num6" => (Keys.NumPad6, 0),
		"numpad7" or "num7" => (Keys.NumPad7, 0),
		"numpad8" or "num8" => (Keys.NumPad8, 0),
		"numpad9" or "num9" => (Keys.NumPad9, 0),
		"add" or "numpadadd" or "numadd" or "numpadplus" or "numplus" => (Keys.Add, 0),
		"numpad-" or "num-" or "subtract" or "numpadsubtract" or "numsubtract" or "minus" or "numpadminus" or "numminus" => (Keys.Subtract, 0),
		"numpad*" or "num*" or "multiply" or "numpadmultiply" or "nummultiply" => (Keys.Multiply, 0),
		"numpad/" or "num/" or "divide" or "numpaddivide" or "numdivide" => (Keys.Divide, 0),
		"numpad." or "num." or "decimal" or "numpaddecimal" or "numdecimal" => (Keys.Decimal, 0),

		_ => Enum.TryParse<Keys>(s, true, out var key) ? (key, (Keystroke.ModifierKeys) 0) : throw new FormatException($"Unknown key: {s}")
	};

	private static void CommandHelp(string[] parameters)
		=> Message($"Commands: {string.Join(" ", Commands.Keys.OrderBy(s => s))}");

	private static void CommandBind(string[] parameters) {
		if (parameters.Length < 2) {
			FailMessage("Usage: .bind <keystroke>[,<keystroke>]* <command> [parameters]");
			return;
		}

		var keystrokes = ParseKeystrokes(parameters[0]);

		for (var i = 0; i < KeyBindings.Count; ++i) {
			var binding = KeyBindings[i];
			if (binding.Keystrokes.SequenceEqual(keystrokes)) {
				FailMessage("That command is already bound.");
				return;
			}
		}

		if (Commands.TryGetValue(parameters[1], out var command)) {
			KeyBindings.Add(new KeyBinding(keystrokes, parameters[1], command, parameters.Skip(2).ToArray()));
			Save();
			SuccessMessage("Command bound successfully.");
		} else
			FailMessage($"Unknown command: {parameters[1]}");
	}

	private static void CommandUnbind(string[] parameters) {
		if (parameters.Length < 1) {
			FailMessage("Usage: .unbind <keystroke>[,<keystroke>]*");
			return;
		}

		var keystrokes = ParseKeystrokes(parameters[0]);

		for (var i = 0; i < KeyBindings.Count; ++i) {
			var binding = KeyBindings[i];
			if (binding.Keystrokes.SequenceEqual(keystrokes)) {
				KeyBindings.RemoveAt(i);
				Save();
				FailMessage("Binding removed successfully.");
				return;
			}
		}

		FailMessage("That command is not bound.");
	}

	private static void CommandListBindings(string[] parameters) {
		if (parameters.Length >= 2) {
			FailMessage("Usage: .listbindings [<keystroke>[,<keystroke>]*]");
			return;
		}

		Keystroke[]? keystrokes = parameters.Length != 0 ? ParseKeystrokes(parameters[0]) : null;

		for (var i = 0; i < KeyBindings.Count; ++i) {
			var binding = KeyBindings[i];
			if (keystrokes is null || binding.Keystrokes.Take(keystrokes.Length).SequenceEqual(keystrokes)) {
				Message(string.Join<Keystroke>(",", binding.Keystrokes) + " is bound to: " + binding.Command + " " + string.Join(" ", binding.Arguments));
			}
		}
	}

	private static void CommandParty(string[] parameters) {
		if (Main.netMode != 1) {
			FailMessage("You can't join a party in single player.");
			return;
		}
		if (Main.teamCooldown != 0) {
			FailMessage("You must wait " + Math.Ceiling((double) Main.teamCooldown / 60D) + (Main.teamCooldown <= 60 ? " second." : " seconds."));
			return;
		}
		if (parameters.Length != 1) {
			FailMessage("Usage: .party none|red|yellow|green|blue");
			return;
		}

		int party;
		switch (parameters[0].ToLower()) {
			case "none": case "n": party = 0; break;
			case "red": case "r": party = 1; break;
			case "yellow": case "y": party = 4; break;
			case "green": case "g": party = 2; break;
			case "blue": case "b": party = 3; break;
			case "pink": case "purple": case "p": party = 3; break;
			default: FailMessage("Usage: .party none|[party]"); return;
		}

		if (Main.player[Main.myPlayer].team != party) {
			FailMessage(party == 0 ? "You are not on a party." : "You are already on that party.");
			return;
		}

		Main.teamCooldown = Main.teamCooldownLen;
		SoundEngine.PlaySound(12, -1, -1, 1);
		Main.player[Main.myPlayer].team = party;
		NetMessage.SendData(45, -1, -1, null, Main.myPlayer, 0f, 0f, 0f, 0);
	}

	private static void CommandPvP(string[] parameters) {
		if (Main.netMode != 1) {
			FailMessage("You can't toggle PvP in single player.");
			return;
		}
		if (Main.teamCooldown != 0) {
			FailMessage("You must wait " + Math.Ceiling(Main.teamCooldown / 60D) + (Main.teamCooldown <= 60 ? " second." : " seconds."));
			return;
		}
		if (parameters.Length != 1) {
			FailMessage("Usage: .pvp off|on|toggle");
			return;
		}

		switch (parameters[0].ToLower()) {
			case "off":
				if (!Main.player[Main.myPlayer].hostile) {
					FailMessage("You already have PvP disabled.");
					return;
				}
				Main.player[Main.myPlayer].hostile = false;
				break;
			case "on":
				if (Main.player[Main.myPlayer].hostile) {
					FailMessage("You already have PvP enabled.");
					return;
				}
				Main.player[Main.myPlayer].hostile = true;
				break;
			case "toggle":
				Main.player[Main.myPlayer].hostile = !Main.player[Main.myPlayer].hostile;
				break;
			default:
				FailMessage("Usage: .pvp off|on|toggle");
				return;
		}

		Main.teamCooldown = Main.teamCooldownLen;
		SoundEngine.PlaySound(12, -1, -1, 1);
		NetMessage.SendData(30, -1, -1, null, Main.myPlayer, 0f, 0f, 0f, 0);
	}
}
