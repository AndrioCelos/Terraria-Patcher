# Terraria Patcher

This is a collection of mods for [_Terraria_](https://www.terraria.org/) that are intended to be compatible with vanilla _Terraria_ version 1.4.

## Usage

Run `TerrariaPatcher.exe` to use the patcher. It will present a list of the available mods and a brief description of each.

Select the mods you want, then click **Patch**. This will create `Terraria.patched.exe` and 'patched' copies of other files as appropriate in the _Terraria_ installation directory. If `ReLogic.dll` was patched, the patched copy _must_ be renamed `ReLogic.dll` for the patches to work.

### Client Commands

A core feature of several mods is client commands. These add commands that can be used via the in-game chat to control the mods' features. They use `.` as the prefix. You can also quickly open the chat with a command prefix by pressing `/` or `.` instead of `Enter`.

Enter `.help` for a list of available commands, and `.help [command name]` for information on a specific command.

You can also bind commands to key combinations for easy use. To set a key binding, enter `.bind [keystrokes] [command]`.

Examples of key bindings:

* `.bind P pause`
* `.bind NumPlus counter +`
* `.bind Ctrl+M music toggle`
* `.bind Ctrl+W,F3 stopwatch hide` – to use this one, press `Ctrl`+`W`, release, then press `F3`.

Note that key bindings will not prevent the base game from processing the same keys.

## Building

Because `ReLogic.dll`, a library used by _Terraria_ and targeted by some of these patches, is an embedded resource in `Terraria.exe`, it must be extracted before the patcher can be built.

1. Ensure that _Terraria_ is installed and has been run at least once to install the XNA framework.
2. Build and run the extractor. This will create `ReLogic.dll` in the _Terraria_ installation directory.
3. Ensure the reference paths in the patcher project are correct.
4. Build and run the patcher.

Terraria is © 2021 Re-Logic. This repository does not contain any of Re-Logic's code or binaries.
