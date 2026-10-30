using DocumentService.Data;
using DocumentService.Models;
using Microsoft.EntityFrameworkCore;

namespace DocumentService.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly DocumentDbContext _context;

    public DocumentRepository(DocumentDbContext context)
    {
        _context = context;
    }

    public async Task<List<Document>> GetAllAsync()
    {
        return await _context.Documents
            .AsNoTracking()
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<Document?> GetByIdAsync(int id)
    {
        return await _context.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<Document> CreateAsync(Document document)
    {
        await _context.Documents.AddAsync(document);

        await _context.SaveChangesAsync();

        return document;
    }

    public async Task<Document?> UpdateAsync(Document document)
    {
        var existingDocument =
            await _context.Documents
                .FirstOrDefaultAsync(d => d.Id == document.Id);

        if (existingDocument == null)
        {
            return null;
        }

        existingDocument.Title = document.Title;
        existingDocument.Description = document.Description;
        existingDocument.Category = document.Category;
        existingDocument.FileName = document.FileName;
        existingDocument.FileType = document.FileType;
        existingDocument.FilePath = document.FilePath;
        existingDocument.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return existingDocument;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var document =
            await _context.Documents
                .FirstOrDefaultAsync(d => d.Id == id);

        if (document == null)
        {
            return false;
        }

        _context.Documents.Remove(document);

        await _context.SaveChangesAsync();

        return true;
    }

    
    public async Task<List<Document>> SearchAsync(
    string? searchTerm,
    DocumentCategory? category)
    {
        var query = _context.Documents.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(d =>
                d.Title.Contains(searchTerm) ||
                d.Description.Contains(searchTerm) ||
                d.FileName.Contains(searchTerm) ||
                d.FileType.Contains(searchTerm));
        }

        if (category.HasValue)
        {
            query = query.Where(d => d.Category == category.Value);
        }

        return await query
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }
}
