#!/bin/sh
set -e

CERT_DIR="/tmp/mtls"
mkdir -p "$CERT_DIR"

write_pem_if_set() {
	var_name="$1"
	dest="$2"
	eval "content=\${$var_name}"
	if [ -n "$content" ]; then
		printf '%b' "$content" > "$dest"
	fi
}

write_pem_if_set MTLS_SERVER_CERT "$CERT_DIR/server.pem"
write_pem_if_set MTLS_SERVER_KEY "$CERT_DIR/server.key"
write_pem_if_set MTLS_CA_CERT "$CERT_DIR/ca.pem"

if [ -f "$CERT_DIR/server.pem" ] && [ -f "$CERT_DIR/server.key" ]; then
	export MTLS_SERVER_CERT_PATH="$CERT_DIR/server.pem"
	export MTLS_SERVER_KEY_PATH="$CERT_DIR/server.key"
	export Mtls__Enabled="true"
fi

if [ -n "$MTLS_CLIENT_CERT_THUMBPRINT" ]; then
	export Mtls__AllowedClientThumbprints__0="$MTLS_CLIENT_CERT_THUMBPRINT"
fi

if [ -n "$PORT" ]; then
	export ASPNETCORE_URLS="http://0.0.0.0:${PORT}"
fi

exec dotnet GLOWAPI.API.dll
