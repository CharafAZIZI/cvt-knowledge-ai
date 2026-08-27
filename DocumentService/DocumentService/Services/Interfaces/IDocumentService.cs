using DocumentService.DTOs;

namespace DocumentService.Services.Interfaces;

public interface IDocumentService
{
    Task<List<DocumentResponse>> GetAllAsync();

    Task<DocumentResponse?> GetByIdAsync(int id);

    Task<DocumentResponse> CreateAsync(
        CreateDocumentRequest request,
        int createdBy);

    Task<DocumentResponse?> UpdateAsync(
        int id,
        UpdateDocumentRequest request);

    Task<bool> DeleteAsync(int id);

    Task<bool> ActivateAsync(int id);

    Task<bool> DeactivateAsync(int id);

    Task<List<DocumentResponse>> SearchAsync(
        string searchTerm);
}
