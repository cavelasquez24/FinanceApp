using FinanceApp.Application.DTOs.Reimbursement;

namespace FinanceApp.Application.Interfaces;

public interface IReimbursementService
{
    Task<IReadOnlyList<ReimbursementResponseDto>> GetAllAsync(Guid userId, DateOnly? startDate, DateOnly? endDate, CancellationToken cancellationToken = default);
    Task<ReimbursementResponseDto> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<ReimbursementResponseDto> CreateAsync(Guid userId, ReimbursementCreateDto dto, CancellationToken cancellationToken = default);
    Task<ReimbursementResponseDto> UpdateAsync(Guid id, Guid userId, ReimbursementUpdateDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<ReimbursementSummaryDto> GetSummaryAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
}
