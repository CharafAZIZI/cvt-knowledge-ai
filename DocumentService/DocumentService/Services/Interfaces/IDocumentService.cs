using DocumentService.DTOs;

namespace DocumentService.Services.Interfaces;

public interface IDocumentService
{
    Task<List<DocumentResponse>> GetAllAsync();

    Task<DocumentResponse?> GetByIdAsync(int id);

    Task<DocumentResponse> CreateAsync(
        CreateDocumentRequest request,
        int createdBy,
        CancellationToken cancellationToken = default);

    Task<DocumentResponse?> UpdateAsync(
        int id,
        UpdateDocumentRequest request,
        IFormFile? file,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id);

    Task<List<DocumentResponse>> SearchAsync(
        string searchTerm);

    Task<(Stream Stream, string FileName, string ContentType)?> DownloadAsync(int id);
}
