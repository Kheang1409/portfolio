# Stage 1: Build
FROM node:24-bullseye-slim AS builder
WORKDIR /app

# Disable Next.js telemetry
ENV NEXT_TELEMETRY_DISABLED=1

# Copy package manifests first to leverage Docker layer caching
COPY package*.json ./

# Install dependencies
RUN if [ -f package-lock.json ]; then \
      npm ci --legacy-peer-deps --no-audit --no-progress; \
    else \
      npm install --legacy-peer-deps --no-audit --no-progress; \
    fi

# Copy source code and build Next.js app
COPY . .
RUN npm run build

# Stage 2: Production
FROM node:24-bullseye-slim AS runner
WORKDIR /app

ENV NODE_ENV=production
ENV PORT=3000
ENV HOSTNAME=0.0.0.0

# Copy build output and static assets
COPY --from=builder --chown=node:node /app/.next/standalone . 
COPY --from=builder --chown=node:node /app/.next/static ./.next/static
COPY --from=builder --chown=node:node /app/public ./public

# Switch to non-root user
USER node

EXPOSE $PORT

# Start the Next.js standalone server
CMD ["node", "server.js"]