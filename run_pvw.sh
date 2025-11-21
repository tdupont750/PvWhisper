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

DEBUG "Script directory: $ROOT_DIR"
DEBUG "Using pipe:       $PIPE_PATH"
DEBUG "Using project:    $PROJECT_PATH"

# Create the named pipe if it doesn't exist
if [[ -e "$PIPE_PATH" && ! -p "$PIPE_PATH" ]]; then
  ERROR "'$PIPE_PATH' exists but is not a named pipe"
  WARN "Deleting '$PIPE_PATH'"
  rm -fr "$PIPE_PATH"
fi

if [[ ! -p "$PIPE_PATH" ]]; then
  DEBUG "Creating named pipe '$PIPE_PATH'"
  mkfifo "$PIPE_PATH"
else
  WARN "Named pipe already exists '$PIPE_PATH'"
fi

# Initalize so dotnet StreamReader can open the file
DEBUG "Writer - Initalizing pipe '$PIPE_PATH'"
(
    if timeout 5 bash -c "echo -n 'i' > '$PIPE_PATH'" >/dev/null 2>&1; then
        DEBUG "Writer - Successfully initialized pipe '$PIPE_PATH'"
        INFO "Ready!"
    else
        ERROR "Writer - Failed to initialize pipe '$PIPE_PATH'"
    fi
) &

# Run the .NET project. Configuration is read from AppConfig.json only.
set +e
dotnet run --project "$PROJECT_PATH"
EXIT_CODE=$?
set -e

DEBUG "PvWhisper exited with code: $EXIT_CODE"

# Clean up: remove the named pipe
if [[ -p "$PIPE_PATH" ]]; then
  DEBUG "Removing named pipe '$PIPE_PATH'"
  rm -f "$PIPE_PATH"
else
  ERROR "Could not find '$PIPE_PATH'"
fi

INFO "Done!"
exit "$EXIT_CODE"
