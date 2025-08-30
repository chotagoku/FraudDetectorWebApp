using FraudDetectorWebApp.Models;

namespace FraudDetectorWebApp.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByIdAsync(int id);
        Task<User> CreateAsync(User user);
        Task<User> UpdateAsync(User user);
        Task<bool> DeleteAsync(int id);
        Task<bool> EmailExistsAsync(string email);
        Task<IEnumerable<User>> GetAllAsync();
        Task<int> CountAsync();
    }
}
