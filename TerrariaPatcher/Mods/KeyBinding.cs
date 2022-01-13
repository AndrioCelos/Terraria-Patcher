#nullable enable

namespace TerrariaPatcher.Mods;

public class KeyBinding {
	public Keystroke[] Keystrokes { get; private set; }
	public CommandAction? Action { get; private set; }
	public string Command { get; private set; }
	public string[] Arguments { get; private set; }

	internal int progress;

	public KeyBinding(Keystroke stroke, string command, CommandAction? action, string[] parameters) : this(new Keystroke[] { stroke }, command, action, parameters) { }
	public KeyBinding(Keystroke[] strokes, string command, CommandAction? action, string[] parameters) {
		this.Keystrokes = strokes;
		this.Command = command;
		this.Action = action;
		this.Arguments = parameters;
	}
}
