# DocumentService — stockage SQL Server + MinIO

## Architecture retenue

Le fichier physique est stocké dans **MinIO**. SQL Server stocke uniquement les métadonnées et la clé de l'objet MinIO.

```text
Document
├── Id              -> SQL Server
├── Title           -> SQL Server
├── Description     -> SQL Server
├── FileName        -> SQL Server
├── FileType        -> SQL Server
├── FilePath        -> SQL Server (object key MinIO)
├── CreatedAt       -> SQL Server
├── UpdatedAt       -> SQL Server
├── CreatedBy       -> SQL Server
└── IsActive        -> SQL Server

MinIO / cvt-documents
└── <GUID>.<extension> -> fichier physique
```

## Flux de création

1. Le contrôleur lit explicitement `multipart/form-data`.
2. Le fichier est envoyé dans MinIO avec une clé unique `GUID + extension`.
3. SQL Server enregistre `Title`, `Description` et la référence `FilePath` vers MinIO.
4. L'indexation IA est lancée après la sauvegarde. Une panne IA ne supprime pas le document SQL/MinIO.
5. Si SQL échoue après l'upload MinIO, le nouveau fichier est supprimé pour éviter un objet orphelin.

## Flux de modification

### Sans nouveau fichier

Seules les métadonnées SQL sont modifiées :

```text
Title / Description -> SQL Server
Fichier MinIO       -> inchangé
```

### Avec nouveau fichier

```text
1. Upload nouveau fichier -> MinIO
2. Mise à jour SQL        -> nouvelle FilePath
3. Suppression ancien     -> MinIO, seulement après succès SQL
4. Ré-indexation IA       -> secondaire
```

Cette séquence évite de supprimer l'ancien fichier avant que SQL ne pointe vers le nouveau.

## Timeout MinIO

Le client MinIO possède maintenant un timeout explicite configurable :

```json
"Minio": {
  "TimeoutSeconds": 60
}
```

Le même réglage est injecté par Docker avec `Minio__TimeoutSeconds=60`.

Les opérations d'upload/download/delete utilisent également le `CancellationToken` de la requête lorsque cela est pertinent.

## Tests Postman

### Création

`POST http://localhost:5000/api/documents`

Body → `form-data` :

| Key | Type | Valeur |
|---|---|---|
| title | Text | Guide sécurité |
| description | Text | Document de test |
| file | File | test.pdf |

Header :

```text
Authorization: Bearer <JWT_ADMIN>
```

Résultat attendu :
- une ligne dans SQL Server avec `Title`, `Description`, `FileName`, `FileType`, `FilePath` ;
- le fichier correspondant existe dans le bucket `cvt-documents` de MinIO.

### Modification des métadonnées uniquement

`PUT http://localhost:5000/api/documents/{id}`

Body → `form-data` :

| Key | Type | Valeur |
|---|---|---|
| title | Text | Nouveau titre |
| description | Text | Nouvelle description |

Résultat attendu : le fichier MinIO reste identique et les deux champs SQL sont mis à jour.

### Modification avec remplacement du fichier

`PUT http://localhost:5000/api/documents/{id}`

Body → `form-data` :

| Key | Type | Valeur |
|---|---|---|
| title | Text | Nouveau titre |
| description | Text | Nouvelle description |
| file | File | nouveau.pdf |

Résultat attendu :
- un nouvel objet apparaît dans MinIO ;
- SQL pointe vers ce nouvel objet ;
- l'ancien objet est supprimé après le succès SQL ;
- la réponse contient les nouvelles métadonnées.

## Diagnostic si un upload bloque

```powershell
docker logs -f cvt-document-service
docker logs -f cvt-minio
docker stats
```

Les logs DocumentService permettent de distinguer :

```text
[UPDATE] Upload MinIO début
[UPDATE] Upload MinIO terminé
[UPDATE] SQL Server SaveChanges début
[UPDATE] SQL Server OK
```

Si le premier apparaît sans le second, l'attente se situe dans l'opération MinIO. Avec le timeout de 60 secondes, l'opération ne doit plus attendre indéfiniment.

## Important

Le code source doit être reconstruit après modification :

```powershell
docker compose build --no-cache document-service
docker compose up -d document-service
```

Puis vérifier :

```powershell
docker logs -f cvt-document-service
```
