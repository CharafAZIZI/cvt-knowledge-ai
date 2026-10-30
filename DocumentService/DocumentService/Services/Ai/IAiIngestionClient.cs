namespace DocumentService.Services.Ai;

public interface IAiIngestionClient
{
    /// <summary>
    /// Déclenche l'indexation (extraction + chunking + embeddings) côté
    /// service IA. Ne doit jamais faire échouer la création du document :
    /// les erreurs sont attrapées et journalisées par l'appelant.
    /// </summary>
    Task NotifyIngestionAsync(
        int documentId,
        string title,
        string fileName,
        Stream fileStream,
        string contentType,
        CancellationToken cancellationToken = default);

    Task NotifyDeletionAsync(
        int documentId,
        CancellationToken cancellationToken = default);
}
