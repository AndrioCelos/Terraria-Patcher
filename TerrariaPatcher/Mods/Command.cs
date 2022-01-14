#nullable enable

using System;

namespace TerrariaPatcher.Mods;

public class Command {
	public delegate void CommandAction(Command command, string label, string[] args);

	public CommandAction Action { get; }
	public int MinParameters { get; }
	public int MaxParameters { get; }
	public string ParametersHint { get; }
	public string Description { get; }

	public Command(CommandAction action, int minParameters, string parametersHint, string description)
		: this(action, minParameters, int.MaxValue, parametersHint, description) { }
	public Command(CommandAction action, int minParameters, int maxParameters, string parametersHint, string description) {
		this.Action = action ?? throw new ArgumentNullException(nameof(action));
		this.MinParameters = minParameters;
		this.MaxParameters = maxParameters;
		this.ParametersHint = parametersHint ?? throw new ArgumentNullException(nameof(parametersHint));
		this.Description = description ?? throw new ArgumentNullException(nameof(description));
	}

	public void ShowParametersFailMessage(string label) => CommandManager.FailMessage($"Usage: {label} {this.ParametersHint}");
}

