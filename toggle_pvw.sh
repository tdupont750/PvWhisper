#!/bin/bash

PORT="${1:-5042}"

curl -s -X POST "http://localhost:${PORT}/command/v" || true
