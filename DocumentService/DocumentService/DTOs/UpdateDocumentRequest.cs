using System.ComponentModel.DataAnnotations;

namespace DocumentService.DTOs;

/// <summary>
/// Métadonnées à mettre à jour. Le fichier (optionnel, pour remplacer le
/// PDF existant) est reçu séparément dans le contrôleur — volontairement
/// pas imbriqué ici, pour éviter toute ambiguïté de binding d'un
/// IFormFile? optionnel à l'intérieur d'un objet complexe.
/// </summary>
public class UpdateDocumentRequest
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;
}
