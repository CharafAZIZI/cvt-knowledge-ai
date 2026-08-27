using DocumentService.DTOs;
using DocumentService.Models;
using DocumentService.Repositories.Interfaces;
using DocumentService.Services.Interfaces;

namespace DocumentService.Services;

public class DocumentServiceImpl : IDocumentService
{
    private readonly IDocumentRepository _repository;

    public DocumentServiceImpl(IDocumentRepository repository)
    {
        _repository = repository;
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
        int createdBy)
    {
        var document = new Document
        {
            Title = request.Title,
            Description = request.Description,
            FileName = request.FileName,
            FileType = request.FileType,
            FilePath = request.FilePath,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var createdDocument = await _repository.CreateAsync(document);
        return MapToResponse(createdDocument);
    }

    public async Task<DocumentResponse?> UpdateAsync(
        int id,
        UpdateDocumentRequest request)
    {
        var document = new Document
        {
            Id = id,
            Title = request.Title,
            Description = request.Description,
            FileName = request.FileName,
            FileType = request.FileType,
            FilePath = request.FilePath
        };

        var updatedDocument = await _repository.UpdateAsync(document);

        return updatedDocument == null
            ? null
            : MapToResponse(updatedDocument);
    }

    public Task<bool> DeleteAsync(int id)
        => _repository.DeleteAsync(id);

    public Task<bool> ActivateAsync(int id)
        => _repository.ActivateAsync(id);

    public Task<bool> DeactivateAsync(int id)
        => _repository.DeactivateAsync(id);

    public async Task<List<DocumentResponse>> SearchAsync(string searchTerm)
    {
        var documents = await _repository.SearchAsync(searchTerm);
        return documents.Select(MapToResponse).ToList();
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
            IsActive = document.IsActive
        };
    }
}
