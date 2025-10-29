FROM node:20-alpine AS build

WORKDIR /app

COPY package*.json ./

RUN npm ci --silent

COPY . .
RUN npm run build --configuration=production

FROM nginx:alpine

WORKDIR /usr/share/nginx/html

RUN rm -rf ./*

COPY --from=build /app/dist/portfolio/ ./

COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

ENTRYPOINT ["nginx", "-g", "daemon off;"]
