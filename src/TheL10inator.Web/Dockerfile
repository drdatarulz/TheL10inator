FROM node:22-alpine AS build
WORKDIR /src

COPY src/TheL10inator.Web/package*.json ./
RUN npm ci

COPY src/TheL10inator.Web/ ./
RUN npm run build -- --configuration production

FROM nginx:1.27-alpine AS runtime
COPY --from=build /src/dist/TheL10inator.Web/browser /usr/share/nginx/html
COPY src/TheL10inator.Web/nginx.conf /etc/nginx/conf.d/default.conf
COPY src/TheL10inator.Web/docker-entrypoint.sh /docker-entrypoint.d/40-runtime-config.sh
RUN chmod +x /docker-entrypoint.d/40-runtime-config.sh

EXPOSE 80
