using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.Infrastructure.Persistence.Repositories;

public class CategoryRepository : BaseRepository<Category>, ICategoryRepository
{
    public CategoryRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Category>> GetByUserIdAsync(
        Guid userId,
        CategoryType? type = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Categories
            .Where(c => c.DeletedAt == null
                // Categorías del sistema (user_id = NULL)
                && (c.UserId == null
                // O categorías propias del usuario
                || c.UserId == userId));

        if (type.HasValue)
            query = query.Where(c => c.Type == type.Value);

        return await query
            .OrderBy(c => c.IsDefault ? 0 : 1) // primero las del sistema
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(
        Guid userId,
        string name,
        CategoryType type,
        CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .AnyAsync(c => c.UserId == userId
                        && c.Name.ToLower() == name.ToLower()
                        && c.Type == type
                        && c.DeletedAt == null,
                cancellationToken);
    }
}