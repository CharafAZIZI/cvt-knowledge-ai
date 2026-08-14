using AuthService.DTOs;
using AuthService.Entities;
using AuthService.Repositories.Interfaces;
using AuthService.Services.Interfaces;

namespace AuthService.Services.Implementations;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(
        IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    // ============================================================
    // GET ALL
    // ============================================================

    public async Task<List<UserResponse>> GetAllAsync()
    {
        var users =
            await _userRepository.GetAllAsync();

        return users
            .Select(MapToResponse)
            .ToList();
    }

    // ============================================================
    // GET BY ID
    // ============================================================

    public async Task<UserResponse?> GetByIdAsync(int id)
    {
        var user =
            await _userRepository.GetByIdAsync(id);

        if (user == null)
        {
            return null;
        }

        return MapToResponse(user);
    }

    // ============================================================
    // CREATE
    // ============================================================

    public async Task<UserResponse?> CreateAsync(
        CreateUserRequest request)
    {
        var email = request.Email
            .Trim()
            .ToLowerInvariant();

        var exists =
            await _userRepository
                .EmailExistsAsync(email);

        if (exists)
        {
            return null;
        }

        var user = new User
        {
            FirstName =
                request.FirstName.Trim(),

            LastName =
                request.LastName.Trim(),

            Email = email,

            // BCrypt
            PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(
                    request.Password),

            Role = "ADMIN",

            IsActive = true,

            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user);

        await _userRepository.SaveChangesAsync();

        return MapToResponse(user);
    }

    // ============================================================
    // UPDATE
    // ============================================================

    public async Task<UserResponse?> UpdateAsync(
        int id,
        UpdateUserRequest request)
    {
        var user =
            await _userRepository.GetByIdAsync(id);

        if (user == null)
        {
            return null;
        }

        var email = request.Email
            .Trim()
            .ToLowerInvariant();

        var emailExists =
            await _userRepository
                .EmailExistsForAnotherUserAsync(
                    email,
                    id);

        if (emailExists)
        {
            throw new InvalidOperationException(
                "Cet email est déjà utilisé.");
        }

        user.FirstName =
            request.FirstName.Trim();

        user.LastName =
            request.LastName.Trim();

        user.Email = email;

        user.UpdatedAt =
            DateTime.UtcNow;

        _userRepository.Update(user);

        await _userRepository.SaveChangesAsync();

        return MapToResponse(user);
    }

    // ============================================================
    // DELETE
    // ============================================================

    public async Task<bool> DeleteAsync(int id)
    {
        var user =
            await _userRepository.GetByIdAsync(id);

        if (user == null)
        {
            return false;
        }

        var activeAdmins =
            await _userRepository
                .CountActiveAdminsAsync();

        if (user.Role == "ADMIN" &&
            user.IsActive &&
            activeAdmins <= 1)
        {
            throw new InvalidOperationException(
                "Impossible de supprimer le dernier administrateur actif.");
        }

        _userRepository.Delete(user);

        await _userRepository.SaveChangesAsync();

        return true;
    }

    // ============================================================
    // ACTIVATE
    // ============================================================

    public async Task<bool> ActivateAsync(int id)
    {
        var user =
            await _userRepository.GetByIdAsync(id);

        if (user == null)
        {
            return false;
        }

        user.IsActive = true;

        user.UpdatedAt =
            DateTime.UtcNow;

        _userRepository.Update(user);

        await _userRepository.SaveChangesAsync();

        return true;
    }

    // ============================================================
    // DEACTIVATE
    // ============================================================

    public async Task<bool> DeactivateAsync(int id)
    {
        var user =
            await _userRepository.GetByIdAsync(id);

        if (user == null)
        {
            return false;
        }

        var activeAdmins =
            await _userRepository
                .CountActiveAdminsAsync();

        if (user.Role == "ADMIN" &&
            user.IsActive &&
            activeAdmins <= 1)
        {
            throw new InvalidOperationException(
                "Impossible de désactiver le dernier administrateur actif.");
        }

        user.IsActive = false;

        user.UpdatedAt =
            DateTime.UtcNow;

        _userRepository.Update(user);

        await _userRepository.SaveChangesAsync();

        return true;
    }

    // ============================================================
    // CHANGE OWN PASSWORD
    // ============================================================

    public async Task<bool> ChangeOwnPasswordAsync(
        int userId,
        ChangePasswordRequest request)
    {
        var user =
            await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            return false;
        }

        var valid =
            BCrypt.Net.BCrypt.Verify(
                request.CurrentPassword,
                user.PasswordHash);

        if (!valid)
        {
            return false;
        }

        user.PasswordHash =
            BCrypt.Net.BCrypt.HashPassword(
                request.NewPassword);

        user.UpdatedAt =
            DateTime.UtcNow;

        _userRepository.Update(user);

        await _userRepository.SaveChangesAsync();

        return true;
    }

    // ============================================================
    // RESET PASSWORD
    // ============================================================

    public async Task<bool> ResetPasswordAsync(
        int userId,
        ResetPasswordRequest request)
    {
        var user =
            await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            return false;
        }

        user.PasswordHash =
            BCrypt.Net.BCrypt.HashPassword(
                request.NewPassword);

        user.UpdatedAt =
            DateTime.UtcNow;

        _userRepository.Update(user);

        await _userRepository.SaveChangesAsync();

        return true;
    }

    // ============================================================
    // MAPPING
    // ============================================================

    private static UserResponse MapToResponse(
        User user)
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