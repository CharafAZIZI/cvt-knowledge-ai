using AuthService.DTOs;

namespace AuthService.Services.Interfaces;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(
        LoginRequest request);

    Task<UserResponse?> GetCurrentUserAsync(
        int userId);
}