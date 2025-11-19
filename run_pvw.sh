#!/bin/bash
set -euo pipefail

# Usage:
#   ./run_pvwhisper_with_pipe.sh [/path/to/pipe] [/path/to/PvWhisper.csproj]
#
# Defaults:
#   PIPE_PATH = /tmp/pvwhisper.fifo
#   PROJECT   = ./PvWhisper/PvWhisper.csproj

# Directory of the currently executing script
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

PIPE_PATH="${1:-/tmp/pvwhisper.fifo}"

# Use the script directory as the root path for the project
PROJECT_PATH="${2:-"$ROOT_DIR/src/PvWhisper/PvWhisper.csproj"}"

echo "[INFO] Script directory: $ROOT_DIR"
echo "[INFO] Using pipe:       $PIPE_PATH"
echo "[INFO] Using project:    $PROJECT_PATH"

# Create the named pipe if it doesn't exist
if [[ -e "$PIPE_PATH" && ! -p "$PIPE_PATH" ]]; then
  echo "[ERROR] '$PIPE_PATH' exists but is not a named pipe."
  exit 1
fi

if [[ ! -p "$PIPE_PATH" ]]; then
  echo "[INFO] Creating named pipe..."
  mkfifo "$PIPE_PATH"
else
  echo "[WARN] Named pipe already exists."
fi

# Optional: show how to send commands to it
echo "[INFO] In another terminal, you can send commands like:"
echo "[INFO] echo -n 'v' > '$PIPE_PATH'   # toggle capture"
echo "[INFO] echo -n 'q' > '$PIPE_PATH'   # quit"

# Run the .NET project. Configuration is read from AppConfig.json only.
set +e
dotnet run --project "$PROJECT_PATH"
EXIT_CODE=$?
set -e

echo "[INFO] PvWhisper exited with code: $EXIT_CODE"

# Clean up: remove the named pipe
if [[ -p "$PIPE_PATH" ]]; then
  echo "[INFO] Removing named pipe '$PIPE_PATH'..."
  rm -f "$PIPE_PATH"
fi

echo "[INFO] Done."
exit "$EXIT_CODE"
