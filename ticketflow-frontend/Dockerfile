# Stage 1: Build Angular
FROM node:22 AS build
WORKDIR /app
COPY package*.json ./
RUN npm install
COPY . .
RUN npm run build -- --configuration production

# Stage 2: Serve with Nginx
FROM nginx:alpine
COPY --from=build /app/dist/ticketflow-frontend/browser /usr/share/nginx/html
# Eğer Angular versiyonunuz eskiyse (16 ve oncesi), ustteki satiri asagidaki ile degistirmeniz gerekebilir:
# COPY --from=build /app/dist/ticketflow-frontend /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
