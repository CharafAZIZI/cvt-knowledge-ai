using AuthService.DTOs;
using AuthService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthService.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(
        IAuthService authService)
    {
        _authService = authService;
    }

    // ============================================================
    // LOGIN
    // ============================================================

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result =
            await _authService.LoginAsync(request);

        if (result == null)
        {
            return Unauthorized(new
            {
                message =
                    "Email ou mot de passe incorrect."
            });
        }

        return Ok(result);
    }

    // ============================================================
    // CURRENT USER
    // ============================================================

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
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

        var user =
            await _authService
                .GetCurrentUserAsync(userId);

        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }
}