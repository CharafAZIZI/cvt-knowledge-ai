using DocumentService.Models;

namespace DocumentService.Repositories.Interfaces;

public interface IDocumentRepository
{
    Task<List<Document>> GetAllAsync();

    Task<Document?> GetByIdAsync(int id);

    Task<Document> CreateAsync(Document document);

    Task<Document?> UpdateAsync(Document document);

    Task<bool> DeleteAsync(int id);

    Task<bool> ActivateAsync(int id);

    Task<bool> DeactivateAsync(int id);

    Task<List<Document>> SearchAsync(string searchTerm);
}