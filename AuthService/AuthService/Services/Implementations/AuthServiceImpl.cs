using AuthService.DTOs;
using AuthService.Repositories.Interfaces;
using AuthService.Services.Interfaces;
using BCrypt.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AuthService.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;

    public AuthService(
        IUserRepository userRepository,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _configuration = configuration;
    }

    // ============================================================
    // LOGIN
    // ============================================================

    public async Task<LoginResponse?> LoginAsync(
        LoginRequest request)
    {
        var email = request.Email
            .Trim()
            .ToLowerInvariant();

        var user =
            await _userRepository.GetByEmailAsync(email);

        if (user == null)
        {
            return null;
        }

        if (!user.IsActive)
        {
            return null;
        }

        // Vérification du mot de passe
        var passwordValid =
            BCrypt.Net.BCrypt.Verify(
                request.Password,
                user.PasswordHash);

        if (!passwordValid)
        {
            return null;
        }

        // Mise à jour de la dernière connexion
        user.LastLoginAt = DateTime.UtcNow;

        _userRepository.Update(user);

        await _userRepository.SaveChangesAsync();

        // Génération du JWT
        var token = GenerateJwtToken(user);

        return new LoginResponse
        {
            Token = token,
            User = MapToResponse(user)
        };
    }

    // ============================================================
    // CURRENT USER
    // ============================================================

    public async Task<UserResponse?> GetCurrentUserAsync(
        int userId)
    {
        var user =
            await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            return null;
        }

        return MapToResponse(user);
    }

    // ============================================================
    // JWT
    // ============================================================

    private string GenerateJwtToken(
        Entities.User user)
    {
        var jwtKey =
            _configuration["Jwt:Key"];

        var jwtIssuer =
            _configuration["Jwt:Issuer"];

        var jwtAudience =
            _configuration["Jwt:Audience"];

        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            throw new InvalidOperationException(
                "JWT Key is not configured.");
        }

        if (string.IsNullOrWhiteSpace(jwtIssuer))
        {
            throw new InvalidOperationException(
                "JWT Issuer is not configured.");
        }

        if (string.IsNullOrWhiteSpace(jwtAudience))
        {
            throw new InvalidOperationException(
                "JWT Audience is not configured.");
        }

        var claims = new List<Claim>
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                user.Id.ToString()),

            new Claim(
                ClaimTypes.Email,
                user.Email),

            new Claim(
                ClaimTypes.Name,
                $"{user.FirstName} {user.LastName}"),

            new Claim(
                ClaimTypes.Role,
                user.Role)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey));

        var credentials =
            new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }

    // ============================================================
    // MAPPING
    // ============================================================

    private static UserResponse MapToResponse(
        Entities.User user)
    {
        return new UserResponse
        {
            Id = user.Id,

            FirstName = user.FirstName,

            LastName = user.LastName,

            Email = user.Email,

            Role = user.Role,

            IsActive = user.IsActive,

            CreatedAt = user.CreatedAt,

            UpdatedAt = user.UpdatedAt,

            LastLoginAt = user.LastLoginAt
        };
    }
}