# CVT Knowledge AI — Frontend Angular

Frontend Angular 17 (standalone components) connecté au backend microservices existant
(API Gateway / AuthService / DocumentService / AiService) via `http://localhost:8080/api`.

## 1. Prérequis

- Node.js 18+ et npm
- Le backend Docker du projet déjà démarré (`docker compose up`) — API Gateway sur le port `8080`.

## 2. Installation

```bash
cd cvt-frontend
npm install
```

## 3. Lancement en développement

```bash
npm start
```

L'application est servie sur **http://localhost:4200**. Elle communique avec le Gateway sur
`http://localhost:8080/api` (voir `src/environments/environment.ts`).

## 4. Build de production

```bash
npm run build
```

Le résultat est généré dans `dist/cvt-frontend/`.

## 5. Compte de test

Le compte administrateur créé par le seeder du backend (`AuthService/Data/DbSeeder.cs`) :

- Email : `admin@cvt.com`
- Mot de passe : `Admin123!`

## 6. Structure du projet

```
src/app/
├── core/
│   ├── guards/          authGuard, adminGuard
│   ├── interceptors/    auth.interceptor (JWT), error.interceptor (401/403/…)
│   ├── models/          interfaces TS alignées sur les DTOs réels du backend
│   ├── services/        AuthService, DocumentService, UserService, AiService, ToastService
│   └── utils/           extraction de message d'erreur (ASP.NET / FastAPI)
├── layouts/
│   └── dashboard-layout/  sidebar rétractable, navbar, menu mobile, profil
├── features/
│   ├── auth/login/
│   ├── dashboard/
│   ├── documents/         + modales ajout/modification, confirmation suppression
│   ├── administration/    + modale ajout/modification (ADMIN uniquement)
│   └── assistant/         chat connecté à POST /api/ai/ask
├── shared/components/
│   ├── toast/              notifications globales
│   └── confirm-modal/      modale de confirmation réutilisable
└── app.routes.ts           routing lazy-loaded + guards
```

## 7. Checklist de vérification manuelle

- [ ] **Login** : connexion avec `admin@cvt.com` / `Admin123!` → redirection vers `/dashboard`.
- [ ] **Login erreur** : mauvais mot de passe → message "Email ou mot de passe incorrect."
- [ ] **Logout** : bouton dans le menu profil → retour à `/login`, token supprimé.
- [ ] **Dashboard** : nombre de documents correct, documents récents affichés (aucune donnée fictive).
- [ ] **Documents — liste** : chargement, recherche avec debounce, pagination si > 10 documents.
- [ ] **Documents — upload** : ajout d'un PDF via la modale, apparaît dans la liste.
- [ ] **Documents — modification** : changer le titre sans remplacer le fichier ; puis remplacer le fichier.
- [ ] **Documents — suppression** : confirmation demandée, document retiré de la liste après confirmation.
- [ ] **Documents — téléchargement** : le fichier se télécharge avec le bon nom.
- [ ] **Administration (ADMIN)** : liste des admins, rôle affiché, création, modification, activation/désactivation, suppression (le dernier admin actif ne peut pas être supprimé/désactivé — message du backend affiché tel quel).
- [ ] **Administration (non-ADMIN)** : lien absent du menu, accès direct à `/administration` redirige vers `/dashboard`.
- [ ] **Assistant IA** : poser une question, réponse affichée avec ses sources (fichier + page) si le backend en renvoie ; Entrée envoie, Maj+Entrée fait un retour à la ligne.
- [ ] **Responsive** : redimensionner en mobile (< 768px) → sidebar remplacée par un menu hamburger, tableaux lisibles.
- [ ] **Session expirée** : après expiration du JWT, un appel API renvoie 401 → déconnexion automatique + message + redirection vers `/login`.

## 8. Points d'attention techniques

- Les réponses de `AiService` (FastAPI) sont en **snake_case** (`file_name`, `page_number`),
  contrairement aux réponses `.NET` qui sont en camelCase — voir `core/models/ai.model.ts`.
- `POST /api/documents` et `PUT /api/documents/{id}` attendent du **multipart/form-data**
  avec exactement les champs `title`, `description`, `file` — voir `DocumentService`.
- Le rôle `ADMIN` est actuellement le seul rôle existant côté backend
  (`AuthService/Entities/User.cs`) ; l'administration gère donc des comptes admin, pas des
  utilisateurs "simples".
- `/api/documents/search` et `/api/documents/{id}/download` passent par la route générique
  `{everything}` de `ocelot.json` : si un souci de routing apparaît sur ces deux endpoints
  via le Gateway, vérifier ce fichier en priorité.
