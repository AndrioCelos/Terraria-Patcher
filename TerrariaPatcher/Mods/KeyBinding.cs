#nullable enable

using System;

using Newtonsoft.Json;

namespace TerrariaPatcher.Mods;

public class KeyBinding {
	[JsonConverter(typeof(KeystrokeJsonConverter))]
	public Keystroke[] Keystrokes { get; private set; }
	public string CommandName { get; private set; }
	[JsonIgnore]
	public Command? Command { get; internal set; }
	public string[] Arguments { get; private set; }

	internal int progress;
	internal bool canReset;

	[JsonConstructor]
	public KeyBinding(Keystroke[] keystrokes, string commandName, string[] arguments) {
		this.Keystrokes = keystrokes ?? throw new ArgumentNullException(nameof(keystrokes));
		this.CommandName = commandName ?? throw new ArgumentNullException(nameof(commandName));
		this.Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
	}
	public KeyBinding(Keystroke[] keystrokes, string commandName, Command? command, string[] arguments) {
		this.Keystrokes = keystrokes ?? throw new ArgumentNullException(nameof(keystrokes));
		this.CommandName = commandName ?? throw new ArgumentNullException(nameof(commandName));
		this.Command = command;
		this.Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
	}

	public void Invoke() {
		if (this.Command is not null) {
			if (this.Arguments.Length < this.Command.MinParameters) {
				CommandManager.FailMessage($"Not enough arguments for command '{this.CommandName}'.");
				this.Command.ShowParametersFailMessage(this.CommandName);
			} else {
				try {
					this.Command.Action(this.Command, this.CommandName, this.Arguments);
				} catch (Exception ex) {
					CommandManager.FailMessage($"The command failed: {ex}");
				}
			}
		} else
			CommandManager.FailMessage($"Bound command '{this.CommandName}' not found.");
	}
}
