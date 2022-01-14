#nullable enable

using System;

namespace TerrariaPatcher.Mods;

public class KeyBinding {
	public Keystroke[] Keystrokes { get; private set; }
	public Command? Command { get; private set; }
	public string CommandLabel { get; private set; }
	public string[] Arguments { get; private set; }

	internal int progress;
	internal bool canReset;

	public KeyBinding(Keystroke stroke, string commandLabel, Command? command, string[] parameters) : this(new Keystroke[] { stroke }, commandLabel, command, parameters) { }
	public KeyBinding(Keystroke[] strokes, string commandLabel, Command? command, string[] parameters) {
		this.Keystrokes = strokes;
		this.CommandLabel = commandLabel;
		this.Command = command;
		this.Arguments = parameters;
	}

	public void Invoke() {
		if (this.Command is not null) {
			if (this.Arguments.Length < this.Command.MinParameters) {
				CommandManager.FailMessage($"Not enough arguments for command '{this.CommandLabel}'.");
				this.Command.ShowParametersFailMessage(this.CommandLabel);
			} else {
				try {
					this.Command.Action(this.Command, this.CommandLabel, this.Arguments);
				} catch (Exception ex) {
					CommandManager.FailMessage($"The command failed: {ex}");
				}
			}
		} else
			CommandManager.FailMessage($"Bound command '{this.CommandLabel}' not found.");
	}
}
