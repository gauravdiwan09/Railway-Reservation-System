using AuthService.Models;

namespace AuthService.Repositories.Interfaces;

public interface IRoleRepository
{
    Task<List<Role>> GetAllAsync();

    Task<Role?> GetByIdAsync(int roleId);

    Task<Role?> GetByNameAsync(string roleName);
}