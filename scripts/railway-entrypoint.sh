#!/bin/sh
set -e

if [ -n "$PORT" ]; then
  export ASPNETCORE_URLS="http://0.0.0.0:${PORT}"
fi

exec dotnet GLOWAPI.API.dll
