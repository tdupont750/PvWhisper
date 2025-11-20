#!/bin/bash
set -euo pipefail

# Usage:
#   ./run_pvwhisper_with_pipe.sh [/path/to/pipe] [/path/to/PvWhisper.csproj]
#
# Defaults:
#   PIPE_PATH = /tmp/pvwhisper.fifo
#   PROJECT   = ./PvWhisper/PvWhisper.csproj

INFO() { echo -e "\e[34m[INFO] $*\e[0m"; }
DEBUG() { echo -e "\e[90m[DEBUG] $*\e[0m"; }
WARN() { echo -e "\e[33m[WARN] $*\e[0m"; }
ERROR() { echo -e "\e[31m[ERROR] $*\e[0m"; }

# Directory of the currently executing script
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

PIPE_PATH="${1:-/tmp/pvwhisper.fifo}"

# Use the script directory as the root path for the project
PROJECT_PATH="${2:-"$ROOT_DIR/src/PvWhisper/PvWhisper.csproj"}"

INFO "Script directory: $ROOT_DIR"
INFO "Using pipe:       $PIPE_PATH"
INFO "Using project:    $PROJECT_PATH"

# Create the named pipe if it doesn't exist
if [[ -e "$PIPE_PATH" && ! -p "$PIPE_PATH" ]]; then
  ERROR "'$PIPE_PATH' exists but is not a named pipe."
  WARN "Deleting '$PIPE_PATH'."
  rm -fr "$PIPE_PATH"
fi

if [[ ! -p "$PIPE_PATH" ]]; then
  INFO "Creating named pipe."
  mkfifo "$PIPE_PATH"
else
  WARN "Named pipe already exists."
fi

# Optional: show how to send commands to it
DEBUG "In another terminal, you can send commands like:"
DEBUG "  echo -n 'v' > '$PIPE_PATH'   # toggle capture"
DEBUG "  echo -n 'q' > '$PIPE_PATH'   # quit"

# Initalize so dotnet StreamReader can open the file
INFO "Initalizing '$PIPE_PATH'"
(
    if timeout 5 echo -n 'i' > "$PIPE_PATH"; then
        INFO "Successfully initalized '$PIPE_PATH'"
    else
        ERROR "Failed to initalized '$PIPE_PATH'"
    fi
) &

# Run the .NET project. Configuration is read from AppConfig.json only.
set +e
dotnet run --project "$PROJECT_PATH"
EXIT_CODE=$?
set -e

INFO "PvWhisper exited with code: $EXIT_CODE"

# Clean up: remove the named pipe
if [[ -p "$PIPE_PATH" ]]; then
  INFO "Removing named pipe '$PIPE_PATH'"
  rm -f "$PIPE_PATH"
else
  ERROR "Could not find '$PIPE_PATH'"
fi

INFO "Done."
exit "$EXIT_CODE"
