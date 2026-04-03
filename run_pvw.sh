#!/bin/bash
set -euo pipefail

# Usage:
#   ./run_pvw.sh [/path/to/PvWhisper.csproj]
#
# Defaults:
#   PROJECT = ./src/PvWhisper/PvWhisper.csproj

INFO() { echo -e "\e[34m[SH] [INFO] $*\e[0m"; }
DEBUG() { echo -e "\e[90m[SH] [DEBUG] $*\e[0m"; }
WARN() { echo -e "\e[33m[SH] [WARN] $*\e[0m"; }
ERROR() { echo -e "\e[31m[SH] [ERROR] $*\e[0m"; }

# Directory of the currently executing script
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Use the script directory as the root path for the project
PROJECT_PATH="${1:-"$ROOT_DIR/src/PvWhisper/PvWhisper.csproj"}"

DEBUG "Using directory: $ROOT_DIR"
DEBUG "Using project:   $PROJECT_PATH"

# Check dotnet is installed
if ! command -v dotnet &>/dev/null; then
  ERROR "dotnet is not installed or not in PATH"
  exit 1
fi

# Check ydotoold daemon is running
if ! pgrep -x ydotoold &>/dev/null; then
  ERROR "ydotoold daemon is not running — start it with: ydotoold &"
  exit 1
fi

# Guard against concurrent instances
LOCK_FILE="/tmp/pvwhisper.lock"
exec 9>"$LOCK_FILE"
if ! flock -n 9; then
  ERROR "Another instance of PvWhisper is already running"
  exit 1
fi

# Run the .NET project. Configuration is read from AppConfig.json only.
set +e
dotnet run --project "$PROJECT_PATH"
EXIT_CODE=$?
set -e

DEBUG "PvWhisper exited with code: $EXIT_CODE"
INFO "Done!"
exit "$EXIT_CODE"
