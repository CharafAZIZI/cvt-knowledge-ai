using AuthService.DTOs;
using AuthService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthService.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "ADMIN")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(
        IUserService userService)
    {
        _userService = userService;
    }

    // ============================================================
    // GET ALL
    // ============================================================

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users =
            await _userService.GetAllAsync();

        return Ok(users);
    }

    // ============================================================
    // GET BY ID
    // ============================================================

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user =
            await _userService.GetByIdAsync(id);

        if (user == null)
        {
            return NotFound(new
            {
                message =
                    "Administrateur introuvable."
            });
        }

        return Ok(user);
    }

    // ============================================================
    // CREATE
    // ============================================================

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var user =
            await _userService.CreateAsync(request);

        if (user == null)
        {
            return Conflict(new
            {
                message =
                    "Un administrateur avec cet email existe déjà."
            });
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = user.Id },
            user);
    }

    // ============================================================
    // UPDATE
    // ============================================================

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateUserRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var user =
                await _userService
                    .UpdateAsync(id, request);

            if (user == null)
            {
                return NotFound();
            }

            return Ok(user);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }

    // ============================================================
    // DELETE
    // ============================================================

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var result =
                await _userService.DeleteAsync(id);

            if (!result)
            {
                return NotFound();
            }

            return Ok(new
            {
                message =
                    "Administrateur supprimé avec succès."
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    // ============================================================
    // ACTIVATE
    // ============================================================

    [HttpPut("{id:int}/activate")]
    public async Task<IActionResult> Activate(int id)
    {
        var result =
            await _userService.ActivateAsync(id);

        if (!result)
        {
            return NotFound();
        }

        return Ok(new
        {
            message =
                "Administrateur activé avec succès."
        });
    }

    // ============================================================
    // DEACTIVATE
    // ============================================================

    [HttpPut("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id)
    {
        try
        {
            var result =
                await _userService
                    .DeactivateAsync(id);

            if (!result)
            {
                return NotFound();
            }

            return Ok(new
            {
                message =
                    "Administrateur désactivé avec succès."
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    // ============================================================
    // CHANGE OWN PASSWORD
    // ============================================================

    [HttpPut("me/password")]
    public async Task<IActionResult>
        ChangeOwnPassword(
            [FromBody] ChangePasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var claim =
            User.FindFirst(
                ClaimTypes.NameIdentifier);

        if (claim == null ||
            !int.TryParse(
                claim.Value,
                out int userId))
        {
            return Unauthorized();
        }

        var result =
            await _userService
                .ChangeOwnPasswordAsync(
                    userId,
                    request);

        if (!result)
        {
            return BadRequest(new
            {
                message =
                    "Le mot de passe actuel est incorrect."
            });
        }

        return Ok(new
        {
            message =
                "Mot de passe modifié avec succès."
        });
    }

    // ============================================================
    // RESET PASSWORD OF ANOTHER ADMIN
    // ============================================================

    [HttpPut("{id:int}/password")]
    public async Task<IActionResult>
        ResetPassword(
            int id,
            [FromBody] ResetPasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result =
            await _userService
                .ResetPasswordAsync(
                    id,
                    request);

        if (!result)
        {
            return NotFound();
        }

        return Ok(new
        {
            message =
                "Mot de passe réinitialisé avec succès."
        });
    }
}