using FinanceApp.Application.DTOs.Tag;

namespace FinanceApp.Application.Interfaces;

public interface ITagService
{
    Task<IReadOnlyList<TagResponseDto>> GetAllAsync(Guid userId, string? search, CancellationToken cancellationToken = default);
    Task<TagResponseDto> CreateAsync(Guid userId, TagCreateDto dto, CancellationToken cancellationToken = default);
    Task<TagResponseDto> UpdateAsync(Guid id, Guid userId, TagUpdateDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<TagResponseDto> MergeAsync(Guid sourceId, Guid userId, TagMergeDto dto, CancellationToken cancellationToken = default);
    Task<TagExpenseReportDto> GetExpenseReportAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
}
