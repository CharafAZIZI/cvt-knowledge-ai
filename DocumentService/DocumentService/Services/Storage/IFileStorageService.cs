namespace DocumentService.Services.Storage;

public interface IFileStorageService
{
    /// <summary>
    /// Upload un fichier vers MinIO et retourne la clé d'objet générée
    /// (à stocker dans Document.FilePath).
    /// </summary>
    Task<string> UploadAsync(
        Stream fileStream,
        string objectKey,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Récupère un fichier depuis MinIO sous forme de flux.
    /// </summary>
    Task<Stream> DownloadAsync(
        string objectKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Supprime un fichier de MinIO.
    /// </summary>
    Task DeleteAsync(
        string objectKey,
        CancellationToken cancellationToken = default);
}
