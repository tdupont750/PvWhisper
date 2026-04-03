#!/bin/bash

PORT="${1:-5000}"

curl -s -X POST "http://localhost:${PORT}/command/v" || true
