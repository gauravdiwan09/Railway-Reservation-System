using AuthService.Data;
using AuthService.Models;
using AuthService.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly AuthDbContext _context;

    public RoleRepository(AuthDbContext context)
    {
        _context = context;
    }

    public async Task<List<Role>> GetAllAsync()
    {
        return await _context.Roles
            .ToListAsync();
    }

    public async Task<Role?> GetByIdAsync(int roleId)
    {
        return await _context.Roles
            .FirstOrDefaultAsync(r => r.RoleId == roleId);
    }

    public async Task<Role?> GetByNameAsync(string roleName)
    {
        return await _context.Roles
            .FirstOrDefaultAsync(r => r.RoleName == roleName);
    }
}