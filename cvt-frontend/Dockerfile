# ---- Étape 1 : build de l'application Angular ----
FROM node:20-alpine AS build

WORKDIR /app

# On copie d'abord uniquement les fichiers de dépendances pour profiter
# du cache Docker (npm ci ne sera relancé que si package*.json change).
COPY package.json package-lock.json ./

RUN npm ci

# On copie ensuite le reste du code source.
COPY . .

# Build de production (sortie dans dist/cvt-frontend/browser avec le
# "application builder" d'Angular 17).
RUN npm run build -- --configuration production


# ---- Étape 2 : image finale, servie par Nginx ----
FROM nginx:1.27-alpine AS final

# Configuration Nginx adaptée à une SPA Angular (fallback sur index.html).
COPY nginx.conf /etc/nginx/conf.d/default.conf

COPY --from=build /app/dist/cvt-frontend/browser /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
