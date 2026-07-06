using FinanceApp.Application.DTOs.Investment;

namespace FinanceApp.Application.Interfaces;

public interface IInvestmentService
{
    Task<IEnumerable<InvestmentResponseDto>> GetAllAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<InvestmentResponseDto> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<InvestmentResponseDto> CreateAsync(
        Guid userId,
        InvestmentCreateDto dto,
        CancellationToken cancellationToken = default);

    Task<InvestmentResponseDto> UpdateAsync(
        Guid id,
        Guid userId,
        InvestmentUpdateDto dto,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<InvestmentSummaryDto> GetSummaryAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<InvestmentRecordResponseDto>> GetRecordsAsync(
        Guid investmentId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<InvestmentRecordResponseDto> AddRecordAsync(
        Guid investmentId,
        Guid userId,
        InvestmentRecordCreateDto dto,
        CancellationToken cancellationToken = default);
}