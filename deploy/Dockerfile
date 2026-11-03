# Railway: set the service's Root Directory to /deploy.
# Copy the Unity WebGL build output into deploy/site/ (index.html, Build/, TemplateData/).
FROM caddy:2.8-alpine
COPY Caddyfile /etc/caddy/Caddyfile
COPY site /srv
