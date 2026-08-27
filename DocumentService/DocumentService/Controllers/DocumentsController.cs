using DocumentService.DTOs;
using DocumentService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DocumentService.Controllers;

[ApiController]
[Route("api/documents")]
[Authorize(Roles = "ADMIN")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;

    public DocumentsController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var documents = await _documentService.GetAllAsync();
        return Ok(documents);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return BadRequest(new
            {
                message = "Le terme de recherche est obligatoire."
            });
        }

        var documents = await _documentService.SearchAsync(searchTerm);
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

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateDocumentRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var claim = User.FindFirst(ClaimTypes.NameIdentifier);

        if (claim == null ||
            !int.TryParse(claim.Value, out var userId))
        {
            return Unauthorized(new
            {
                message = "Administrateur non identifié."
            });
        }

        var document = await _documentService.CreateAsync(
            request,
            userId);

        return CreatedAtAction(
            nameof(GetById),
            new { id = document.Id },
            document);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateDocumentRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var document = await _documentService.UpdateAsync(id, request);

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

    [HttpPut("{id:int}/activate")]
    public async Task<IActionResult> Activate(int id)
    {
        var result = await _documentService.ActivateAsync(id);

        if (!result)
        {
            return NotFound(new
            {
                message = "Document introuvable."
            });
        }

        return Ok(new
        {
            message = "Document activé avec succès."
        });
    }

    [HttpPut("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var result = await _documentService.DeactivateAsync(id);

        if (!result)
        {
            return NotFound(new
            {
                message = "Document introuvable."
            });
        }

        return Ok(new
        {
            message = "Document désactivé avec succès."
        });
    }
}
