# ── Stage 1: Build frontend ────────────────────────────────────────────────
FROM node:20-alpine AS frontend
WORKDIR /build
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci --silent
COPY frontend/ ./
RUN npm run build

# ── Stage 2: Build Go binary ───────────────────────────────────────────────
FROM golang:1.21-alpine AS go-build
WORKDIR /build
COPY api/ ./
RUN CGO_ENABLED=0 go build \
      -ldflags="-s -w" \
      -o /Macroverse42 .

# ── Stage 3: Runtime image ─────────────────────────────────────────────────
FROM alpine:latest
RUN apk add --no-cache ca-certificates tzdata

WORKDIR /app

# Binary + UI
COPY --from=go-build  /Macroverse42        ./Macroverse42
COPY --from=frontend  /build/dist/         ./frontend-build/

# Required config and splash
COPY shader-preview-settings.default.json  ./shader-preview-settings.json
COPY ascii.art                             ./ascii.art

# Starter shader library only (full personal libraries stay private/local)
COPY shader-index.factory.json             ./shader-index.json
COPY shaders/starter-pack/                 ./shaders/starter-pack/

EXPOSE 8765

CMD ["./Macroverse42"]
