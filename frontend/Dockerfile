FROM node:20-alpine AS build
WORKDIR /app

COPY frontend/tide-dashboard/package*.json ./
RUN npm install

COPY frontend/tide-dashboard/ .
RUN npm run build

FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY frontend/tide-dashboard/nginx.conf /etc/nginx/conf.d/default.conf
COPY frontend/tide-dashboard/nginx-entrypoint.sh /nginx-entrypoint.sh
RUN chmod +x /nginx-entrypoint.sh
EXPOSE 80
ENTRYPOINT ["/nginx-entrypoint.sh"]
