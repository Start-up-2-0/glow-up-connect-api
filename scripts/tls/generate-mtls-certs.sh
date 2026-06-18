#!/usr/bin/env bash
# Gera PKI interna para mTLS Caddy (cliente) -> API (servidor).
# Uso: ./scripts/tls/generate-mtls-certs.sh [diretorio_saida]
# Nao commite os arquivos .pem gerados.

set -euo pipefail

OUT_DIR="${1:-./certs/mtls-dev}"
VALID_DAYS="${MTLS_VALID_DAYS:-730}"
CN_CA="Glow Up Connect Internal CA"
CN_SERVER="glowapi.internal"
CN_CLIENT="glow-caddy-bff"

mkdir -p "$OUT_DIR"

echo "Gerando CA em $OUT_DIR ..."
openssl genrsa -out "$OUT_DIR/ca.key" 4096
openssl req -x509 -new -nodes -key "$OUT_DIR/ca.key" -sha256 -days "$VALID_DAYS" \
  -subj "/CN=$CN_CA" -out "$OUT_DIR/ca.pem"

write_cert_ext() {
  local path="$1"
  local san_dns="$2"
  local eku="$3"
  cat > "$path" <<EOF
basicConstraints = CA:FALSE
keyUsage = digitalSignature, keyEncipherment
extendedKeyUsage = ${eku}
subjectAltName = DNS:${san_dns}
EOF
}

echo "Gerando certificado do servidor (API) ..."
SERVER_EXT="$OUT_DIR/server-ext.cnf"
write_cert_ext "$SERVER_EXT" "$CN_SERVER" "serverAuth"
openssl genrsa -out "$OUT_DIR/server.key" 2048
openssl req -new -key "$OUT_DIR/server.key" -subj "/CN=$CN_SERVER" -out "$OUT_DIR/server.csr"
openssl x509 -req -in "$OUT_DIR/server.csr" -CA "$OUT_DIR/ca.pem" -CAkey "$OUT_DIR/ca.key" \
  -CAcreateserial -out "$OUT_DIR/server.pem" -days "$VALID_DAYS" -sha256 -extfile "$SERVER_EXT"

echo "Gerando certificado do cliente (Caddy BFF) ..."
CLIENT_EXT="$OUT_DIR/client-ext.cnf"
write_cert_ext "$CLIENT_EXT" "$CN_CLIENT" "clientAuth"
openssl genrsa -out "$OUT_DIR/client.key" 2048
openssl req -new -key "$OUT_DIR/client.key" -subj "/CN=$CN_CLIENT" -out "$OUT_DIR/client.csr"
openssl x509 -req -in "$OUT_DIR/client.csr" -CA "$OUT_DIR/ca.pem" -CAkey "$OUT_DIR/ca.key" \
  -CAcreateserial -out "$OUT_DIR/client.pem" -days "$VALID_DAYS" -sha256 -extfile "$CLIENT_EXT"

THUMBPRINT=$(openssl x509 -in "$OUT_DIR/client.pem" -noout -fingerprint -sha1 | cut -d= -f2 | tr -d ':')

rm -f "$OUT_DIR"/*.csr "$OUT_DIR"/*-ext.cnf "$OUT_DIR"/ca.srl

cat <<EOF

Certificados gerados em: $OUT_DIR

Railway (API):
  MTLS_SERVER_CERT=<conteudo de server.pem>
  MTLS_SERVER_KEY=<conteudo de server.key>
  MTLS_CA_CERT=<conteudo de ca.pem>
  MTLS_CLIENT_CERT_THUMBPRINT=$THUMBPRINT

Railway (App/Caddy):
  MTLS_CLIENT_CERT=<conteudo de client.pem>
  MTLS_CLIENT_KEY=<conteudo de client.key>
  MTLS_CA_CERT=<conteudo de ca.pem>
  MTLS_SERVER_NAME=glowapi.internal
  API_INTERNAL_URL=https://<servico-api>.railway.internal:8443

Rotacione os certificados antes de expirar ($VALID_DAYS dias).
EOF
