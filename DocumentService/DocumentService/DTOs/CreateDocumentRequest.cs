using DocumentService.Models;
using System.ComponentModel.DataAnnotations;

namespace DocumentService.DTOs;

/// <summary>
/// Métadonnées envoyées avec le fichier lors de l'upload (multipart/form-data).
/// FileName, FileType et FilePath sont déduits automatiquement du fichier
/// reçu, ils ne sont plus saisis manuellement par le client.
/// </summary>
public class CreateDocumentRequest
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public DocumentCategory Category { get; set; }

    [Required]
    public IFormFile File { get; set; } = null!;
}
