# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

PvWhisper is a .NET 9.0 cross-platform Speech-to-Text console application that uses OpenAI's Whisper AI model to transcribe spoken audio and output to console, clipboard, or virtual keyboard (via ydotool).

## Build and Test Commands

```bash
# Build
dotnet build src/PvWhisper.sln --configuration Release

# Run tests
dotnet test src/PvWhisper.sln --configuration Release

# Run a single test (by name filter)
dotnet test src/PvWhisper.sln --filter "FullyQualifiedName~TestMethodName"

# Run the application (creates named pipe, starts app)
./run_pvw.sh

# Toggle capture (sends 'v' to named pipe)
./toggle_pvw.sh
```

## Architecture

The app is structured around an async command-processing loop in `PvWhisperApp.cs`. Commands flow in from multiple sources through a `System.Threading.Channels` pipeline, and audio capture state is managed with locks.

**Data flow:**
1. `Program.cs` — constructs all dependencies and loads `AppConfig.json`
2. `CommandChannelFactory` — creates an unbounded channel fed by stdin and/or a named pipe
3. `PvWhisperApp.ProcessCommandsAsync` — main event loop; handles `v/c/z/x/q` commands
4. `CaptureManager` — captures PCM16 audio frames via PvRecorder into a thread-safe buffer
5. `WhisperTranscriber` — converts buffered frames to WAV (16kHz), runs Whisper inference, applies text transforms
6. `TextTransformer` / `RegexReplacer` — ordered transformation pipeline on the resulting text
7. `OutputDispatcher` — routes final text to configured publishers (Console, Clipboard, Ydotool)

**Key commands (stdin or named pipe):**
- `v` — toggle capture (start, or stop+transcribe)
- `c` — start capture
- `x` — stop and transcribe
- `z` — stop and discard
- `q` — quit

## Configuration

`AppConfig.json` (root dir, copied to build output) controls all runtime behavior:
- `outputs` — array of output targets: `Console`, `Clipboard`, `Ydotool`
- `deviceIndex` / `deviceName` — audio device selection
- `modelType` — Whisper model size (`Tiny`, `Base`, `Small`, `Medium`, `Large`)
- `modelDir` — path to Whisper model files (downloaded automatically from HuggingFace if missing)
- `captureTimeoutSeconds` — auto-stop after N seconds of capture
- `pipePath` — named pipe path for IPC (default `/tmp/pvwhisper.fifo`)
- `textTransforms` — ordered list of `{find, replace, isRegex, caseSensitive}` rules; `replace` supports group references like `{1:ToUpper}`

## Project Structure

```
src/
  PvWhisper/
    Audio/          # CaptureManager, DeviceResolver, WavConverter, CaptureTimeoutManager
    Config/         # AppConfig (DTO), ConfigService (loads/validates JSON)
    Input/          # CommandChannelFactory, ConsoleCommandSource, PipeCommandSource
    Logging/        # Logger (color-coded, debug/info/warn/error)
    Output/         # OutputDispatcher, publishers (Console, Clipboard, Ydotool)
    Text/           # TextTransformer, RegexReplacer
    Transcription/  # WhisperTranscriber, ModelEnsurer
    Program.cs      # Entry point and DI wiring
    PvWhisperApp.cs # Main app loop
  PvWhisper.Tests/
    RegexReplacerTests.cs  # xUnit tests for regex replacement logic
```

## Key Design Patterns

- **Constructor injection** throughout — no service locator or statics
- **Interfaces** on all major components (`ICaptureManager`, `IWhisperTranscriber`, `IOutputDispatcher`, etc.) for testability
- **Named pipe IPC** — the pipe is pre-opened with a sentinel `i` character to avoid blocking on `open()`
- `CaptureTimeoutManager` uses `CancellationToken` to auto-cancel long captures; cancellation flows back through the command channel
