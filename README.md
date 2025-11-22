# PvWhisper

A cross platform Speech to Text console application.

Build using:
- [dotnet](https://dotnet.microsoft.com/en-us/download)
- [PvRecorder](https://github.com/Picovoice/pvrecorder)
- [Whisper.net](https://github.com/sandrohanea/whisper.net)
- [ydotool](https://github.com/ReimuNotMoe/ydotool)

Authored using AI via [JetBrains Junie](https://www.jetbrains.com/junie/)

## Features

- **Write to Console**
	- Prints transcribed Speech to Text out to the console
	- Works on Linux, Mac, and Windows

- **Write to Clipboard**
	- Copy transcribed Speech to Text to the system clipboard
	- Works on Linux, Mac, and Windows

- **Write to Virtual Keyboard (ydotool)**
	- Type transcribed Speech to Text into a virtual keyboard
	- Only available on Linux with [ydotool](https://github.com/ReimuNotMoe/ydotool)  

- **Use any Whisper Model**
	- Multiple [Whisper Model](https://whisper-api.com/blog/models/) sizes supported
	- Automatically downloads models from [Hugging Face](https://huggingface.co/openai/models)

- **Toggle speech capture via named pipe**
	- Create global shortcut to toggle speech capture via shell script
	- Works on Linux, Mac, and Windows with [WSL](https://learn.microsoft.com/en-us/windows/wsl/install)

- **Text transformations with Regular Expressions**
	- Create any number of transform rules
	- Use plain text or regular expressions

## Getting Started

1. Install the [dotnet SDK](https://dotnet.microsoft.com/en-us/download)
2. Clone this repository
3. Configure via `AppConfig.json` 
4. Execute `run_pvw.sh` 
5. Toggle capture by executing `toggle_pvw.sh`

## Configuration

Configure by updating the JSON values found in the [AppConfig.json](https://github.com/tdupont750/PvWhisper/blob/main/AppConfig.json) file

Supported Outputs:
- `Console`
- `Clipboard`
- `ydotool`

See source for [OutputTargets](https://github.com/tdupont750/PvWhisper/blob/main/src/PvWhisper/Config/OutputTarget.cs) and [ModelKinds](https://github.com/tdupont750/PvWhisper/blob/main/src/PvWhisper/Config/ModelKind.cs)

Text transformations will run in the order provided

## Commands

Console Commands:
- `v` = toggle capture (start / stop + transcribe)
- `c` = start capture
- `z` = stop capture and discard audio
- `x` = stop capture and transcribe
- `q` = quit

In another terminal, you can send commands:
- `echo -n 'v' > '$PIPE_PATH'   # toggle capture`
- `echo -n 'q' > '$PIPE_PATH'   # quit`

## License

Release under the GNU General Public License v3.0
