#!/bin/bash
set -euo pipefail

export YDOTOOL_SOCKET=/tmp/.ydotool_socket

sudo ydotoold --socket-own=1000:1000
