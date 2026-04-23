FROM node:20-alpine AS deps
WORKDIR /app
COPY package.json ./
RUN npm install --omit=dev --no-audit --no-fund

FROM node:20-alpine
WORKDIR /app

# Drop root — run as non-privileged user
RUN addgroup -S audit && adduser -S audit -G audit
USER audit

COPY --from=deps --chown=audit:audit /app/node_modules ./node_modules
COPY --chown=audit:audit package.json ./
COPY --chown=audit:audit src/ ./src/

EXPOSE 8080
CMD ["node", "src/index.js"]
