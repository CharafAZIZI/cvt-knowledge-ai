using DocumentService.DTOs;
using DocumentService.Models;
using DocumentService.Services.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DocumentService.Controllers;

[ApiController]
[Route("api/documents")]
[Authorize] // authentifié requis par défaut ; ADMIN uniquement sur les actions d'écriture
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IDocumentService documentService,
        ILogger<DocumentsController> logger)
    {
        _documentService = documentService;
        _logger = logger;
    }

    // ============================================================
    // LECTURE — accessible à tout utilisateur authentifié (ADMIN ou USER)
    // ============================================================

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] DocumentCategory? category)
    {
        var documents = category.HasValue
            ? await _documentService.SearchAsync(null, category)
            : await _documentService.GetAllAsync();

        return Ok(documents);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string? searchTerm,
        [FromQuery] DocumentCategory? category)
    {
        if (string.IsNullOrWhiteSpace(searchTerm) && !category.HasValue)
        {
            return BadRequest(new { message = "Un terme de recherche ou une catégorie est requis." });
        }

        var documents = await _documentService.SearchAsync(searchTerm, category);
        return Ok(documents);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var document = await _documentService.GetByIdAsync(id);

        if (document == null)
        {
            return NotFound(new
            {
                message = "Document introuvable."
            });
        }

        return Ok(document);
    }

    [HttpGet("{id:int}/download")]
    public async Task<IActionResult> Download(int id)
    {
        var result = await _documentService.DownloadAsync(id);

        if (result == null)
        {
            return NotFound(new
            {
                message = "Document introuvable."
            });
        }

        var (stream, fileName, contentType) = result.Value;

        // Empêche tout cache client (navigateur, proxy) : l'URL reste la
        // même après un remplacement de fichier, il ne faut jamais servir
        // une version obsolète.
        Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        Response.Headers.Pragma = "no-cache";

        return File(
            stream,
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            fileName);
    }

    // ============================================================
    // ÉCRITURE — réservée à ADMIN
    // ============================================================

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(100 * 1024 * 1024)]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        // Lecture explicite du multipart/form-data. On évite ainsi de
        // dépendre du model binder pour les métadonnées + IFormFile.
        var form = await Request.ReadFormAsync(cancellationToken);

        var title = form["title"].ToString().Trim();
        var description = form["description"].ToString();
        var categoryRaw = form["category"].ToString();
        var file = form.Files["file"];

        _logger.LogInformation(
            "[CREATE] title='{Title}' | descriptionLength={DescriptionLength} | file={FileInfo}",
            title,
            description.Length,
            file == null ? "NULL" : $"{file.FileName} ({file.Length} octets)");

        if (string.IsNullOrWhiteSpace(title))
        {
            return BadRequest(new { message = "Le titre est obligatoire." });
        }

        if (title.Length > 200)
        {
            return BadRequest(new { message = "Le titre ne doit pas dépasser 200 caractères." });
        }

        if (description.Length > 1000)
        {
            return BadRequest(new { message = "La description ne doit pas dépasser 1000 caractères." });
        }

        if (!Enum.TryParse<DocumentCategory>(categoryRaw, out var category))
        {
            return BadRequest(new { message = "Catégorie invalide." });
        }

        if (file == null)
        {
            return BadRequest(new { message = "Le fichier est obligatoire." });
        }

        if (file.Length == 0)
        {
            return BadRequest(new { message = "Le fichier est vide." });
        }

        var claim = User.FindFirst(ClaimTypes.NameIdentifier);

        if (claim == null || !int.TryParse(claim.Value, out var userId))
        {
            return Unauthorized(new { message = "Administrateur non identifié." });
        }

        var request = new CreateDocumentRequest
        {
            Title = title,
            Description = description,
            Category = category,
            File = file
        };

        var document = await _documentService.CreateAsync(request, userId, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = document.Id },
            document);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "ADMIN")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(100 * 1024 * 1024)]
    public async Task<IActionResult> Update(
        int id,
        [FromForm] string title,
        [FromForm] string description,
        [FromForm] string category,
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken)
    {
            _logger.LogInformation(
                "[UPDATE] id={Id} - début | title='{Title}' | descriptionLength={DescriptionLength} | file={FileInfo}",
                id,
                title,
                description?.Length ?? 0,
                file == null
                    ? "NULL"
                    : $"{file.FileName} ({file.Length} octets)");

            title = title?.Trim() ?? string.Empty;
            description ??= string.Empty;

            if (string.IsNullOrWhiteSpace(title))
            {
                return BadRequest(new
                {
                    message = "Le titre est obligatoire."
                });
            }

            if (title.Length > 200)
            {
                return BadRequest(new
                {
                    message = "Le titre ne doit pas dépasser 200 caractères."
                });
            }

            if (description.Length > 1000)
            {
                return BadRequest(new
                {
                    message = "La description ne doit pas dépasser 1000 caractères."
                });
            }

            if (!Enum.TryParse<DocumentCategory>(category, out var parsedCategory))
            {
                return BadRequest(new { message = "Catégorie invalide." });
            }

            if (file != null && file.Length == 0)
                {
                    return BadRequest(new
                    {
                        message = "Le fichier reçu est vide."
                    });
            }

            _logger.LogInformation(
                "[UPDATE] id={Id} | binding OK | title='{Title}' | description='{Description}' | file={FileInfo}",
                id,
                title,
                description,
                file == null
                    ? "NULL"
                    : $"{file.FileName} ({file.Length} octets)");

            var request = new UpdateDocumentRequest
            {
                Title = title,
                Description = description,
                Category = parsedCategory
            };

            var document = await _documentService.UpdateAsync(
                id,
                request,
                file,
                cancellationToken);

            if (document == null)
            {
                return NotFound(new
                {
                    message = "Document introuvable."
                });
            }

            return Ok(document);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _documentService.DeleteAsync(id);

        if (!result)
        {
            return NotFound(new
            {
                message = "Document introuvable."
            });
        }

        return Ok(new
        {
            message = "Document supprimé avec succès."
        });
    }
}
