#!/bin/bash

FIFO_FILE='/tmp/pvwhisper.fifo'

# If it doesn't exist or is not a FIFO, do nothing
[[ -p "$FIFO_FILE" ]] || exit 0

# Abort if write can't proceed
timeout 1 bash -c "echo -n 'v' > '$FIFO_FILE'" >/dev/null 2>&1 || true
