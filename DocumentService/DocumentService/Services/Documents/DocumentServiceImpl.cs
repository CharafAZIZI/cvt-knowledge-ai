using DocumentService.DTOs;
using DocumentService.Models;
using DocumentService.Repositories;
using DocumentService.Services.Ai;
using DocumentService.Services.Storage;

namespace DocumentService.Services.Documents;

public class DocumentServiceImpl : IDocumentService
{
    private readonly IDocumentRepository _repository;
    private readonly IFileStorageService _fileStorage;
    private readonly IAiIngestionClient _aiIngestionClient;
    private readonly ILogger<DocumentServiceImpl> _logger;

    public DocumentServiceImpl(
        IDocumentRepository repository,
        IFileStorageService fileStorage,
        IAiIngestionClient aiIngestionClient,
        ILogger<DocumentServiceImpl> logger)
    {
        _repository = repository;
        _fileStorage = fileStorage;
        _aiIngestionClient = aiIngestionClient;
        _logger = logger;
    }

    public async Task<List<DocumentResponse>> GetAllAsync()
    {
        var documents = await _repository.GetAllAsync();
        return documents.Select(MapToResponse).ToList();
    }

    public async Task<DocumentResponse?> GetByIdAsync(int id)
    {
        var document = await _repository.GetByIdAsync(id);
        return document == null ? null : MapToResponse(document);
    }

    public async Task<DocumentResponse> CreateAsync(
        CreateDocumentRequest request,
        int createdBy,
        CancellationToken cancellationToken = default)
    {
        if (request.File == null || request.File.Length == 0)
            throw new ArgumentException("Le fichier est obligatoire et ne doit pas être vide.");

        var extension = Path.GetExtension(request.File.FileName);
        var objectKey = $"{Guid.NewGuid():N}{extension}";

        _logger.LogInformation(
            "[CREATE] Upload MinIO début : objectKey={ObjectKey}, file={FileName}, size={Size}",
            objectKey,
            request.File.FileName,
            request.File.Length);

        try
        {
            await using (var stream = request.File.OpenReadStream())
            {
                await _fileStorage.UploadAsync(
                    stream,
                    objectKey,
                    request.File.ContentType,
                    cancellationToken);
            }

            _logger.LogInformation("[CREATE] Upload MinIO terminé : {ObjectKey}", objectKey);

            var document = new Document
            {
                Title = request.Title.Trim(),
                Description = request.Description ?? string.Empty,
                Category = request.Category,
                FileName = request.File.FileName,
                FileType = string.IsNullOrWhiteSpace(request.File.ContentType)
                    ? "application/octet-stream"
                    : request.File.ContentType,
                FilePath = objectKey,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            try
            {
                var createdDocument = await _repository.CreateAsync(document);

                _logger.LogInformation(
                    "[CREATE] SQL Server OK : documentId={DocumentId}, title='{Title}', descriptionLength={DescriptionLength}",
                    createdDocument.Id,
                    createdDocument.Title,
                    createdDocument.Description.Length);

                // L'IA est secondaire : une panne d'indexation ne doit pas
                // annuler un document déjà enregistré correctement.
                try
                {
                    await using var ingestStream = request.File.OpenReadStream();
                    await _aiIngestionClient.NotifyIngestionAsync(
                        createdDocument.Id,
                        createdDocument.Title,
                        createdDocument.FileName,
                        ingestStream,
                        createdDocument.FileType,
                        cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Échec de l'indexation IA du document {DocumentId}. Le document reste disponible.",
                        createdDocument.Id);
                }

                return MapToResponse(createdDocument);
            }
            catch
            {
                // SQL a échoué : le fichier MinIO ne doit pas rester orphelin.
                try
                {
                    await _fileStorage.DeleteAsync(objectKey, CancellationToken.None);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogError(
                        cleanupEx,
                        "Impossible de supprimer le fichier MinIO orphelin {ObjectKey} après échec SQL.",
                        objectKey);
                }

                throw;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Échec de création du document. objectKey={ObjectKey}",
                objectKey);
            throw;
        }
    }

    public async Task<DocumentResponse?> UpdateAsync(
        int id,
        UpdateDocumentRequest request,
        IFormFile? file,
        CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetByIdAsync(id);

        if (existing == null)
            return null;

        existing.Title = request.Title.Trim();
        existing.Description = request.Description ?? string.Empty;
        existing.Category = request.Category;
        var fileWasReplaced = file != null && file.Length > 0;
        var oldObjectKey = existing.FilePath;
        string? newObjectKey = null;

        _logger.LogInformation(
            "[UPDATE] id={Id} | title='{Title}' | descriptionLength={DescriptionLength} | replaceFile={ReplaceFile}",
            id,
            existing.Title,
            existing.Description.Length,
            fileWasReplaced);

        // Phase 1 : stockage du nouveau fichier.
        // L'ancien objet reste volontairement intact jusqu'à ce que SQL soit
        // enregistré avec succès.
        if (fileWasReplaced)
        {
            var extension = Path.GetExtension(file!.FileName);
            newObjectKey = $"{Guid.NewGuid():N}{extension}";

            _logger.LogInformation(
                "[UPDATE] Upload MinIO début : old={OldObjectKey}, new={NewObjectKey}, file={FileName}, size={Size}",
                oldObjectKey,
                newObjectKey,
                file.FileName,
                file.Length);

            try
            {
                await using var stream = file.OpenReadStream();
                await _fileStorage.UploadAsync(
                    stream,
                    newObjectKey,
                    file.ContentType,
                    cancellationToken);

                _logger.LogInformation(
                    "[UPDATE] Upload MinIO terminé : {NewObjectKey}",
                    newObjectKey);

                existing.FileName = file.FileName;
                existing.FileType = string.IsNullOrWhiteSpace(file.ContentType)
                    ? "application/octet-stream"
                    : file.ContentType;
                existing.FilePath = newObjectKey;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Échec upload MinIO du nouveau fichier du document {DocumentId}.",
                    id);
                throw;
            }
        }

        Document updatedDocument;

        // Phase 2 : SQL Server.
        try
        {
            _logger.LogInformation(
                "[UPDATE] SQL Server SaveChanges début : id={Id}, title='{Title}', descriptionLength={DescriptionLength}, filePath='{FilePath}'",
                id,
                existing.Title,
                existing.Description.Length,
                existing.FilePath);

            var result = await _repository.UpdateAsync(existing);

            if (result == null)
            {
                if (newObjectKey != null)
                {
                    await TryDeleteObjectAsync(
                        newObjectKey,
                        id,
                        "document introuvable en SQL après upload");
                }

                return null;
            }

            updatedDocument = result;

            _logger.LogInformation(
                "[UPDATE] SQL Server OK : id={Id}, title='{Title}', descriptionLength={DescriptionLength}, filePath='{FilePath}'",
                updatedDocument.Id,
                updatedDocument.Title,
                updatedDocument.Description.Length,
                updatedDocument.FilePath);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            if (newObjectKey != null)
            {
                await TryDeleteObjectAsync(
                    newObjectKey,
                    id,
                    "nouveau fichier après annulation avant validation SQL");
            }

            throw;
        }
        catch (Exception ex)
        {
            // SQL a échoué : l'ancien fichier reste valide, donc le nouveau
            // objet doit être supprimé pour éviter un objet orphelin.
            if (newObjectKey != null)
            {
                await TryDeleteObjectAsync(
                    newObjectKey,
                    id,
                    "nouveau fichier après échec SQL");
            }

            _logger.LogError(
                ex,
                "Échec de mise à jour SQL du document {DocumentId}.",
                id);
            throw;
        }

        // Phase 3 : nettoyage de l'ancien objet uniquement APRÈS que SQL
        // pointe vers le nouveau fichier.
        if (fileWasReplaced &&
            !string.IsNullOrWhiteSpace(oldObjectKey) &&
            !string.Equals(oldObjectKey, newObjectKey, StringComparison.Ordinal))
        {
            await TryDeleteObjectAsync(
                oldObjectKey,
                id,
                "ancien fichier après mise à jour SQL réussie");
        }

        // Phase 4 : indexation IA. Cette phase est secondaire et ne doit pas
        // rendre le couple SQL/MinIO incohérent si l'IA échoue ou si le client
        // annule sa requête.
        if (fileWasReplaced)
        {
            try
            {
                await using var ingestStream = file!.OpenReadStream();
                await _aiIngestionClient.NotifyIngestionAsync(
                    updatedDocument.Id,
                    updatedDocument.Title,
                    updatedDocument.FileName,
                    ingestStream,
                    updatedDocument.FileType,
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "Indexation IA annulée pour le document {DocumentId}. SQL/MinIO restent cohérents.",
                    updatedDocument.Id);
                // Le document est déjà correctement sauvegardé. On ne supprime
                // surtout pas le nouveau fichier ici.
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Échec de la ré-indexation IA du document {DocumentId}. SQL/MinIO restent cohérents.",
                    updatedDocument.Id);
            }
        }

        return MapToResponse(updatedDocument);
    }

    private async Task TryDeleteObjectAsync(
        string objectKey,
        int documentId,
        string reason)
    {
        try
        {
            await _fileStorage.DeleteAsync(objectKey, CancellationToken.None);
            _logger.LogInformation(
                "[MINIO] objet supprimé : {ObjectKey} | document={DocumentId} | raison={Reason}",
                objectKey,
                documentId,
                reason);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "[MINIO] échec suppression objet {ObjectKey} | document={DocumentId} | raison={Reason}",
                objectKey,
                documentId,
                reason);
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var document = await _repository.GetByIdAsync(id);

        if (document == null)
            return false;

        // On supprime d'abord la référence métier en SQL. Si la suppression
        // MinIO échoue ensuite, on a au pire un objet orphelin, mais jamais une
        // ligne SQL qui pointe vers un fichier déjà supprimé.
        var deleted = await _repository.DeleteAsync(id);

        if (!deleted)
            return false;

        try
        {
            await _fileStorage.DeleteAsync(document.FilePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Le document {DocumentId} a été supprimé de SQL mais l'objet MinIO {ObjectKey} n'a pas pu être supprimé.",
                id,
                document.FilePath);
        }

        try
        {
            await _aiIngestionClient.NotifyDeletionAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Échec du retrait de l'index IA pour le document {DocumentId}.",
                id);
        }

        return true;
    }

    public async Task<List<DocumentResponse>> SearchAsync(string? searchTerm, DocumentCategory? category)
    {
        var documents = await _repository.SearchAsync(searchTerm, category);
        return documents.Select(MapToResponse).ToList();
    }

    public async Task<(Stream Stream, string FileName, string ContentType)?> DownloadAsync(int id)
    {
        var document = await _repository.GetByIdAsync(id);

        if (document == null)
            return null;

        var stream = await _fileStorage.DownloadAsync(document.FilePath);

        return (stream, document.FileName, document.FileType);
    }

    private static DocumentResponse MapToResponse(Document document)
    {
        return new DocumentResponse
        {
            Id = document.Id,
            Title = document.Title,
            Description = document.Description,
            FileName = document.FileName,
            FileType = document.FileType,
            FilePath = document.FilePath,
            CreatedAt = document.CreatedAt,
            UpdatedAt = document.UpdatedAt,
            CreatedBy = document.CreatedBy,
            IsActive = document.IsActive,
            Category = document.Category
        };
    }
}
