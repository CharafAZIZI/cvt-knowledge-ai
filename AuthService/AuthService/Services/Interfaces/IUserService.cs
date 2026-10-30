using AuthService.DTOs;

namespace AuthService.Services.Interfaces;

public interface IUserService
{
    Task<List<UserResponse>> GetAllAsync();

    Task<UserResponse?> GetByIdAsync(int id);

    Task<UserResponse?> CreateAsync(
        CreateUserRequest request);

    Task<UserResponse?> UpdateAsync(
        int id,
        UpdateUserRequest request);

    Task<bool> DeleteAsync(int id);

    Task<bool> ActivateAsync(int id);

    Task<bool> DeactivateAsync(int id);

    Task<bool> ChangeOwnPasswordAsync(
        int userId,
        ChangePasswordRequest request);

    Task<bool> ResetPasswordAsync(
        int userId,
        ResetPasswordRequest request);
}