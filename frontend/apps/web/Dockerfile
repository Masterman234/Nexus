# Multi-stage build for the Nexus web frontend.
#
# IMPORTANT: build context must be the `frontend/` directory (the pnpm workspace
# root that holds pnpm-lock.yaml + pnpm-workspace.yaml), NOT frontend/apps/web.
#   docker build -f apps/web/Dockerfile -t nexus-web ./frontend
# The docker-compose / CI build wiring sets this context explicitly.

# ---- Stage 1: build the static assets ----
FROM node:20-alpine AS build
WORKDIR /app

# Enable pnpm via corepack (lockfile is v9 -> pnpm 9).
RUN corepack enable && corepack prepare pnpm@9 --activate

# Copy workspace manifests first for better layer caching.
COPY pnpm-lock.yaml pnpm-workspace.yaml ./
COPY apps/web/package.json apps/web/package.json

# Install only the web app's dependencies, using the frozen lockfile.
RUN pnpm install --frozen-lockfile --filter web...

# Copy the rest of the web app source and build.
COPY apps/web ./apps/web
RUN pnpm --filter web build

# ---- Stage 2: serve via nginx ----
FROM nginx:alpine AS final

# SPA + reverse-proxy config (proxies /api and /chatHub to the API container).
COPY apps/web/nginx.conf /etc/nginx/conf.d/default.conf

# Built assets.
COPY --from=build /app/apps/web/dist /usr/share/nginx/html

EXPOSE 80
# nginx:alpine's default CMD already runs nginx in the foreground.
