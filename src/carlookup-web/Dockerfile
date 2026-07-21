# Build context is this folder: docker build src/carlookup-web

# ---- build ----------------------------------------------------------------
FROM node:22-alpine AS build
WORKDIR /source

# Install from the lockfile first so npm install is cached until dependencies change.
COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

# ---- runtime --------------------------------------------------------------
FROM nginx:1.29-alpine AS final

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /source/dist/carlookup-web/browser /usr/share/nginx/html

EXPOSE 80

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --quiet --spider http://localhost/health || exit 1
