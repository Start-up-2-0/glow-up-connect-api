#!/usr/bin/env bash
# Reemite server.pem com SAN (DNS:glowapi.internal) usando CA e server.key existentes.
# Use apos erro Go/Caddy: "certificate relies on legacy Common Name field, use SANs instead"
# Uso: ./scripts/tls/regenerate-server-cert-san.sh [diretorio_com_ca_e_server.key]

set -euo pipefail

OUT_DIR="${1:-./certs/mtls-staging}"
VALID_DAYS="${MTLS_VALID_DAYS:-730}"
CN_SERVER="${MTLS_SERVER_NAME:-glowapi.internal}"

for f in ca.pem ca.key server.key; do
  if [[ ! -f "$OUT_DIR/$f" ]]; then
    echo "Arquivo ausente: $OUT_DIR/$f" >&2
    exit 1
  fi
done

SERVER_EXT="$OUT_DIR/server-ext.cnf"
cat > "$SERVER_EXT" <<EOF
basicConstraints = CA:FALSE
keyUsage = digitalSignature, keyEncipherment
extendedKeyUsage = serverAuth
subjectAltName = DNS:${CN_SERVER}
EOF

echo "Reemitindo server.pem com SAN DNS:${CN_SERVER} em $OUT_DIR ..."
openssl req -new -key "$OUT_DIR/server.key" -subj "/CN=$CN_SERVER" -out "$OUT_DIR/server.csr"
openssl x509 -req -in "$OUT_DIR/server.csr" -CA "$OUT_DIR/ca.pem" -CAkey "$OUT_DIR/ca.key" \
  -CAcreateserial -out "$OUT_DIR/server.pem" -days "$VALID_DAYS" -sha256 -extfile "$SERVER_EXT"

rm -f "$OUT_DIR/server.csr" "$SERVER_EXT" "$OUT_DIR/ca.srl"

echo "OK. Atualize apenas MTLS_SERVER_CERT no Railway (API). CA e client certs permanecem iguais."
