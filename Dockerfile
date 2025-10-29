# Stage 1: Build Angular application (smaller base & better cache)
FROM node:20-alpine AS build

WORKDIR /app

# Copy package manifests first to install dependencies and leverage cache
COPY package*.json ./

# Use npm ci for reproducible installs and faster builds
RUN npm ci --silent

# Copy source and build
COPY . .
RUN npm run build --configuration=production

# Stage 2: Serve with Nginx (lightweight)
FROM nginx:alpine

WORKDIR /usr/share/nginx/html

# Remove default Nginx HTML files
RUN rm -rf ./*

# Copy built Angular files from build stage
COPY --from=build /app/dist/portfolio/ ./

# Copy custom Nginx configuration
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

ENTRYPOINT ["nginx", "-g", "daemon off;"]
