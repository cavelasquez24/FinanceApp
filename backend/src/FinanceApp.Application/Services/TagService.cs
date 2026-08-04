using System.Text.RegularExpressions;
using FinanceApp.Application.DTOs.Tag;
using FinanceApp.Application.Interfaces;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.Interfaces.Repositories;

namespace FinanceApp.Application.Services;

public class TagService : ITagService
{
    private const int MaxActiveTags = 100;
    private readonly ITagRepository _tagRepository;

    public TagService(ITagRepository tagRepository) => _tagRepository = tagRepository;

    public async Task<IReadOnlyList<TagResponseDto>> GetAllAsync(Guid userId, string? search, CancellationToken cancellationToken = default) =>
        (await _tagRepository.GetByUserAsync(userId, search, cancellationToken)).Select(Map).ToList();

    public async Task<TagResponseDto> CreateAsync(Guid userId, TagCreateDto dto, CancellationToken cancellationToken = default)
    {
        var name = CleanName(dto.Name);
        Validate(name, dto.Color);
        var normalized = Normalize(name);
        if (await _tagRepository.ExistsActiveAsync(userId, normalized, cancellationToken: cancellationToken))
            throw new DomainException("TAG_ALREADY_EXISTS", $"Ya existe una etiqueta llamada '{name}'");
        if (await _tagRepository.CountActiveAsync(userId, cancellationToken) >= MaxActiveTags)
            throw new DomainException("TAG_LIMIT_REACHED", $"Solo puedes tener {MaxActiveTags} etiquetas activas");

        var tag = await _tagRepository.CreateAsync(new Tag
        {
            UserId = userId,
            Name = name,
            NormalizedName = normalized,
            Color = NormalizeColor(dto.Color)
        }, cancellationToken);
        return Map(tag);
    }

    public async Task<TagResponseDto> UpdateAsync(Guid id, Guid userId, TagUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var tag = await GetOwnedAsync(id, userId, cancellationToken);
        var name = CleanName(dto.Name);
        Validate(name, dto.Color);
        var normalized = Normalize(name);
        if (await _tagRepository.ExistsActiveAsync(userId, normalized, id, cancellationToken))
            throw new DomainException("TAG_ALREADY_EXISTS", $"Ya existe una etiqueta llamada '{name}'");

        tag.Name = name;
        tag.NormalizedName = normalized;
        tag.Color = NormalizeColor(dto.Color);
        tag.UpdatedAt = DateTimeOffset.UtcNow;
        await _tagRepository.UpdateAsync(tag, cancellationToken);
        return Map(tag);
    }

    public async Task DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var tag = await GetOwnedAsync(id, userId, cancellationToken);
        tag.DeletedAt = DateTimeOffset.UtcNow;
        tag.UpdatedAt = DateTimeOffset.UtcNow;
        await _tagRepository.UpdateAsync(tag, cancellationToken);
    }

    public async Task<TagResponseDto> MergeAsync(Guid sourceId, Guid userId, TagMergeDto dto, CancellationToken cancellationToken = default)
    {
        if (sourceId == dto.TargetTagId)
            throw new DomainException("INVALID_TAG_MERGE", "Una etiqueta no puede fusionarse consigo misma");
        var source = await GetOwnedAsync(sourceId, userId, cancellationToken);
        var target = await GetOwnedAsync(dto.TargetTagId, userId, cancellationToken);
        await _tagRepository.MergeAsync(source, target, cancellationToken);
        return Map(target);
    }

    public async Task<TagExpenseReportDto> GetExpenseReportAsync(
        Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        if (endDate < startDate)
            throw new DomainException("INVALID_DATE_RANGE", "La fecha final debe ser posterior a la inicial");
        var metrics = await _tagRepository.GetExpenseReportAsync(userId, startDate, endDate, cancellationToken);
        var coverage = await _tagRepository.GetCoverageAsync(userId, startDate, endDate, cancellationToken);
        return new TagExpenseReportDto
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalExpenses = coverage.TotalExpenses,
            TaggedExpenses = coverage.TaggedExpenses,
            Tags = metrics.Select(x => new TagExpenseReportItemDto
            {
                TagId = x.TagId,
                Name = x.Name,
                Color = x.Color,
                TotalAmount = x.TotalAmount,
                ExpenseCount = x.ExpenseCount,
                AverageAmount = x.ExpenseCount == 0 ? 0 : Math.Round(x.TotalAmount / x.ExpenseCount, 2)
            }).ToList()
        };
    }

    private async Task<Tag> GetOwnedAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        var tag = await _tagRepository.GetByIdAsync(id, cancellationToken);
        if (tag == null || tag.UserId != userId || tag.IsDeleted)
            throw new NotFoundException("Etiqueta", id);
        return tag;
    }

    private static string CleanName(string name) => Regex.Replace(name?.Trim() ?? string.Empty, @"\s+", " ");
    private static string Normalize(string name) => name.ToLowerInvariant();
    private static string? NormalizeColor(string? color) => string.IsNullOrWhiteSpace(color) ? null : color.ToUpperInvariant();

    private static void Validate(string name, string? color)
    {
        if (name.Length is < 1 or > 50)
            throw new DomainException("INVALID_TAG_NAME", "El nombre debe tener entre 1 y 50 caracteres");
        if (!string.IsNullOrWhiteSpace(color) && !Regex.IsMatch(color, "^#[0-9a-fA-F]{6}$"))
            throw new DomainException("INVALID_TAG_COLOR", "El color debe tener formato hexadecimal #RRGGBB");
    }

    private static TagResponseDto Map(Tag tag) => new()
    {
        Id = tag.Id,
        Name = tag.Name,
        Color = tag.Color,
        UsageCount = tag.ExpenseTags.Count(et => et.Expense.DeletedAt == null),
        LastUsedAt = tag.LastUsedAt
    };
}
