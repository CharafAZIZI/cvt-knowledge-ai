using System.ComponentModel.DataAnnotations;

namespace DocumentService.DTOs;

public class UpdateDocumentRequest
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string FileType { get; set; } = string.Empty;

    [StringLength(500)]
    public string FilePath { get; set; } = string.Empty;
}