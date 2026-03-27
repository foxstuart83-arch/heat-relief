FROM nginx:1.27-alpine

# Use custom nginx config that listens on port 8080 (required for Cloud Run)
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copy static game files
COPY index.html style.css game.js /usr/share/nginx/html/

EXPOSE 8080

CMD ["nginx", "-g", "daemon off;"]
