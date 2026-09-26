# ---------- build ----------
FROM node:22 AS build
WORKDIR /app

COPY frontend/package*.json ./
RUN npm ci

COPY frontend/. .
RUN npm run build

# ---------- runtime ----------
FROM node:22-slim
WORKDIR /app
ENV NODE_ENV=production
COPY --from=build /app ./
EXPOSE 3000
CMD ["npm","start"]

