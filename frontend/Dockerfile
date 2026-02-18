# Build stage
FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm install
COPY . .
RUN npm run build

# Production stage
FROM nginx:stable-alpine
COPY --from=build /app/dist /usr/share/nginx/html

# Create nginx configuration template that supports environment variables.
# BACKEND_HOST must be the backend's FQDN (e.g. backend.internal....) so TLS/SNI
# matches the backend's certificate when proxying (required for Azure Container Apps).
RUN echo 'server { \
    listen 80; \
    location / { \
        root /usr/share/nginx/html; \
        index index.html index.htm; \
        try_files $uri $uri/ /index.html; \
    } \
    location /api { \
        proxy_pass ${API_URL}; \
        proxy_http_version 1.1; \
        proxy_set_header Host ${BACKEND_HOST}; \
        proxy_set_header Upgrade $http_upgrade; \
        proxy_set_header Connection "upgrade"; \
        proxy_cache_bypass $http_upgrade; \
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for; \
        proxy_set_header X-Forwarded-Proto $scheme; \
        proxy_ssl_server_name on; \
        proxy_ssl_name ${BACKEND_HOST}; \
    } \
    location /planningpokerhub { \
        proxy_pass ${API_URL}; \
        proxy_http_version 1.1; \
        proxy_set_header Host ${BACKEND_HOST}; \
        proxy_set_header Upgrade $http_upgrade; \
        proxy_set_header Connection "upgrade"; \
        proxy_cache_bypass $http_upgrade; \
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for; \
        proxy_set_header X-Forwarded-Proto $scheme; \
        proxy_ssl_server_name on; \
        proxy_ssl_name ${BACKEND_HOST}; \
    } \
}' > /etc/nginx/conf.d/default.conf.template

EXPOSE 80

# Substitute API_URL and BACKEND_HOST. Derive BACKEND_HOST from API_URL if unset
# (hostname only) so TLS/SNI matches the backend in ACA. Strip trailing slash from
# API_URL to avoid double-slash in upstream paths (e.g. //sessions).
CMD ["/bin/sh", "-c", "API_URL=\"${API_URL%/}\"; export BACKEND_HOST=\"${BACKEND_HOST:-$(echo $API_URL | sed -e 's|https\\?://||' -e 's|/.*||' -e 's|:.*||')}\"; export API_URL; envsubst '${API_URL} ${BACKEND_HOST}' < /etc/nginx/conf.d/default.conf.template > /etc/nginx/conf.d/default.conf && nginx -g 'daemon off;'"]
