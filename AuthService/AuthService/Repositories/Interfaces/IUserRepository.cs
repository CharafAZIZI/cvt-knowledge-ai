using AuthService.Entities;

namespace AuthService.Repositories.Interfaces;

public interface IUserRepository
{
    Task<List<User>> GetAllAsync();

    Task<User?> GetByIdAsync(int id);

    Task<User?> GetByEmailAsync(string email);

    Task<bool> EmailExistsAsync(string email);

    Task<bool> EmailExistsForAnotherUserAsync(
        string email,
        int userId);

    Task<int> CountActiveAdminsAsync();

    Task AddAsync(User user);

    void Update(User user);

    void Delete(User user);

    Task SaveChangesAsync();
}