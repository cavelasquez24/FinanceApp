using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Interfaces.Repositories;
using FinanceApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Infrastructure.Persistence.Repositories;

public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(
        string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Where(u => u.Email == email.ToLower() && u.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(
        string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AnyAsync(u => u.Email == email.ToLower()
                        && u.DeletedAt == null, cancellationToken);
    }
}