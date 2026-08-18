using FinanceApp.Domain.Entities;

namespace FinanceApp.Domain.Interfaces.Repositories;

public interface IReimbursementRepository : IBaseRepository<Reimbursement>
{
    Task<IReadOnlyList<Reimbursement>> GetByUserIdAsync(Guid userId, DateOnly? startDate, DateOnly? endDate, CancellationToken cancellationToken = default);
    Task<Reimbursement?> GetByIdWithDetailsAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<Reimbursement?> GetByIdempotencyKeyAsync(Guid userId, Guid idempotencyKey, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalByExpenseIdAsync(Guid userId, Guid expenseId, Guid? excludingId, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalByDateRangeAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
}
