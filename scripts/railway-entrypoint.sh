#!/bin/sh
set -e

CERT_DIR="/tmp/mtls"
mkdir -p "$CERT_DIR"

MTLS_MUTUAL_TLS_PORT="${MTLS_MUTUAL_TLS_PORT:-8443}"

write_pem_if_set() {
	var_name="$1"
	dest="$2"
	eval "content=\${$var_name}"
	if [ -z "$content" ]; then
		return 1
	fi

	printf '%b' "$content" > "$dest"
	if ! grep -q 'BEGIN' "$dest"; then
		echo "glowapi: PEM invalido em ${var_name} (esperado -----BEGIN ...-----)." >&2
		return 1
	fi
	chmod 600 "$dest"
	return 0
}

mtls_server_loaded=false
if write_pem_if_set MTLS_SERVER_CERT "$CERT_DIR/server.pem" \
	&& write_pem_if_set MTLS_SERVER_KEY "$CERT_DIR/server.key"; then
	mtls_server_loaded=true
	write_pem_if_set MTLS_CA_CERT "$CERT_DIR/ca.pem" || true
	export Mtls__ServerCertificatePath="$CERT_DIR/server.pem"
	export Mtls__ServerCertificateKeyPath="$CERT_DIR/server.key"
	export Mtls__Enabled="true"
	export Mtls__MutualTlsPort="$MTLS_MUTUAL_TLS_PORT"
elif [ -n "${MTLS_SERVER_CERT:-}" ] || [ -n "${MTLS_SERVER_KEY:-}" ]; then
	echo "glowapi: MTLS_SERVER_CERT/KEY definidos mas PEM invalido. Revise as variaveis no Railway." >&2
	exit 1
fi

if [ -n "$MTLS_CLIENT_CERT_THUMBPRINT" ]; then
	export Mtls__AllowedClientThumbprints__0="$MTLS_CLIENT_CERT_THUMBPRINT"
fi

if [ "$MTLS_REQUIRED" = "true" ] || [ "$MTLS_REQUIRED" = "1" ]; then
	if [ "$mtls_server_loaded" != "true" ]; then
		echo "glowapi: MTLS_REQUIRED=true mas certificados do servidor nao foram carregados." >&2
		exit 1
	fi
fi

public_port="${PORT:-8080}"
if [ "$mtls_server_loaded" = "true" ]; then
	echo "glowapi: mTLS ativo | HTTP publico=:${public_port} | mTLS=:${MTLS_MUTUAL_TLS_PORT}" >&2
else
	echo "glowapi: mTLS inativo | HTTP=:${public_port}" >&2
fi

if [ -n "$PORT" ]; then
	export ASPNETCORE_URLS="http://0.0.0.0:${PORT}"
fi

exec dotnet GLOWAPI.API.dll
