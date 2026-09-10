using DocumentService.Models;

namespace DocumentService.DTOs;

public class DocumentResponse
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string FileType { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int CreatedBy { get; set; }

    public bool IsActive { get; set; }

    public DocumentCategory Category { get; set; }  
}