#!/bin/sh
set -e

echo "glowapi: aplicando migrations pendentes (pre-deploy)..." >&2
dotnet GLOWAPI.API.dll --migrate-only
echo "glowapi: pre-deploy migrations concluidas." >&2
