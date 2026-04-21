#!/bin/sh
# Runtime config injection for the Angular SPA container.
# Resolves ${API_UPSTREAM} in the nginx config so we can point the web container
# at different API hosts per environment without rebuilding the image.
set -eu

: "${API_UPSTREAM:=http://api:8080}"
export API_UPSTREAM

envsubst '${API_UPSTREAM}' < /etc/nginx/conf.d/default.conf > /etc/nginx/conf.d/default.conf.tmp
mv /etc/nginx/conf.d/default.conf.tmp /etc/nginx/conf.d/default.conf
