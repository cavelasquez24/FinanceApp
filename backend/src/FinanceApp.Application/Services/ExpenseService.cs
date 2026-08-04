using FinanceApp.Application.DTOs.Common;
using FinanceApp.Application.DTOs.Expense;
using FinanceApp.Application.DTOs.Tag;
using FinanceApp.Application.Interfaces;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.Interfaces.Repositories;
using FinanceApp.Domain.Models;

namespace FinanceApp.Application.Services;

public class ExpenseService : IExpenseService
{
    private readonly IExpenseRepository _expenseRepository;
    private readonly IFinancialAccountService _accountService;
    private readonly ITagRepository _tagRepository;

    public ExpenseService(IExpenseRepository expenseRepository, ITagRepository tagRepository, IFinancialAccountService accountService)
    {
        _expenseRepository = expenseRepository;
        _tagRepository = tagRepository;
        _accountService = accountService;
    }

    public async Task<PagedResponse<ExpenseResponseDto>> GetAllAsync(
        Guid userId,
        ExpenseFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        PaymentMethod? paymentMethod = string.IsNullOrWhiteSpace(filter.PaymentMethod)
            ? null
            : Enum.Parse<PaymentMethod>(filter.PaymentMethod.Replace("_", ""), true);
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);
        var options = new ExpenseQueryOptions
        {
            Page = page,
            PageSize = pageSize,
            CategoryId = filter.CategoryId,
            StartDate = filter.StartDate,
            EndDate = filter.EndDate,
            MinAmount = filter.MinAmount,
            MaxAmount = filter.MaxAmount,
            Search = filter.Search,
            PaymentMethod = paymentMethod,
            IsRecurring = filter.IsRecurring,
            SortBy = filter.SortBy,
            SortDescending = !filter.SortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase),
            TagIds = filter.TagIds.Distinct().ToArray(),
            MatchAllTags = filter.TagMatch.Equals("all", StringComparison.OrdinalIgnoreCase),
            Untagged = filter.Untagged,
            Merchant = filter.Merchant
        };
        var (items, totalCount) = await _expenseRepository.GetFilteredAsync(
            userId, options, cancellationToken);

        return new PagedResponse<ExpenseResponseDto>
        {
            Items = items.Select(MapToResponseDto),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ExpenseResponseDto> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var expense = await _expenseRepository.GetByIdAsync(id, cancellationToken);

        if (expense == null || expense.UserId != userId || expense.IsDeleted)
            throw new NotFoundException("Gasto", id);

        return MapToResponseDto(expense);
    }

    public async Task<ExpenseResponseDto> CreateAsync(
        Guid userId,
        ExpenseCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        var tags = await ResolveTagsAsync(userId, dto.TagIds, cancellationToken);
        // Convertimos el string del DTO al enum del Domain
        var paymentMethod = Enum.Parse<PaymentMethod>(
            dto.PaymentMethod.Replace("_", ""),
            ignoreCase: true);

        RecurrenceType? recurrenceType = dto.RecurrenceType != null
            ? Enum.Parse<RecurrenceType>(
                dto.RecurrenceType.Replace("_", ""),
                ignoreCase: true)
            : null;

        var expense = new Expense
        {
            UserId = userId,
            CategoryId = dto.CategoryId,
            AccountId = dto.AccountId,
            Amount = dto.Amount,
            Merchant = dto.Merchant?.Trim(),
            ExpenseTags = tags.Select(t => new ExpenseTag { TagId = t.Id }).ToList(),
            Description = dto.Description?.Trim(),
            Date = dto.Date,
            PaymentMethod = paymentMethod,
            IsRecurring = dto.IsRecurring,
            RecurrenceType = recurrenceType,
            Notes = dto.Notes?.Trim()
        };

        await _expenseRepository.CreateAsync(expense, cancellationToken);
        await _accountService.SyncMovementAsync(
            userId, dto.AccountId, FinancialAccountType.Cash, -dto.Amount,
            dto.Date, "expense", expense.Id,
            dto.Description?.Trim() ?? "Gasto",
            cancellationToken);
        return await GetByIdAsync(expense.Id, userId, cancellationToken);
    }

    public async Task<ExpenseResponseDto> UpdateAsync(
        Guid id,
        Guid userId,
        ExpenseUpdateDto dto,
        CancellationToken cancellationToken = default)
    {
        var tags = await ResolveTagsAsync(userId, dto.TagIds, cancellationToken);
        var expense = await _expenseRepository.GetByIdAsync(id, cancellationToken);

        if (expense == null || expense.UserId != userId || expense.IsDeleted)
            throw new NotFoundException("Gasto", id);

        expense.Merchant = dto.Merchant?.Trim();
        expense.CategoryId = dto.CategoryId;
        expense.AccountId = dto.AccountId;
        expense.Amount = dto.Amount;
        expense.Description = dto.Description?.Trim();
        expense.Date = dto.Date;
        var paymentMethod = Enum.Parse<PaymentMethod>(
                   dto.PaymentMethod.Replace("_", ""),
                   ignoreCase: true);
        expense.PaymentMethod = paymentMethod;
        expense.IsRecurring = dto.IsRecurring;
        RecurrenceType? recurrenceType = dto.RecurrenceType != null
            ? Enum.Parse<RecurrenceType>(
                dto.RecurrenceType.Replace("_", ""),
                ignoreCase: true)
            : null;
        expense.Notes = dto.Notes?.Trim();
        expense.ExpenseTags.Clear();
        foreach (var tag in tags)
            expense.ExpenseTags.Add(new ExpenseTag { ExpenseId = expense.Id, TagId = tag.Id });

        await _expenseRepository.UpdateAsync(expense, cancellationToken);
        await _accountService.SyncMovementAsync(
            userId, dto.AccountId, FinancialAccountType.Cash, -dto.Amount,
            dto.Date, "expense", expense.Id,
            dto.Description?.Trim() ?? "Gasto",
            cancellationToken);
        return await GetByIdAsync(expense.Id, userId, cancellationToken);
    }

    public async Task DeleteAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var expense = await _expenseRepository.GetByIdAsync(id, cancellationToken);

        if (expense == null || expense.UserId != userId || expense.IsDeleted)
            throw new NotFoundException("Gasto", id);

        expense.DeletedAt = DateTimeOffset.UtcNow;
        await _expenseRepository.UpdateAsync(expense, cancellationToken);
        await _accountService.SyncMovementAsync(
            userId, expense.AccountId, FinancialAccountType.Cash, 0,
            expense.Date, "expense", expense.Id, "Gasto eliminado",
            cancellationToken);
    }

    public async Task<ExpenseSummaryDto> GetSummaryAsync(
        Guid userId,
        int month,
        int year,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _expenseRepository.GetByUserIdAsync(
            userId,
            page: 1,
            pageSize: int.MaxValue,
            startDate: new DateOnly(year, month, 1),
            endDate: new DateOnly(year, month, DateTime.DaysInMonth(year, month)),
            cancellationToken: cancellationToken);

        var expenseList = items.ToList();
        var totalAmount = expenseList.Sum(e => e.Amount);

        var byCategory = expenseList
            .GroupBy(e => new
            {
                e.CategoryId,
                e.Category.Name,
                e.Category.Color,
                e.Category.Icon
            })
            .Select(g => new ExpenseByCategoryDto
            {
                CategoryName = g.Key.Name,
                CategoryColor = g.Key.Color,
                CategoryIcon = g.Key.Icon,
                Amount = g.Sum(e => e.Amount),
                Percentage = totalAmount > 0
                    ? Math.Round(g.Sum(e => e.Amount) * 100 / totalAmount, 2)
                    : 0
            })
            .OrderByDescending(x => x.Amount)
            .ToList();

        return new ExpenseSummaryDto
        {
            TotalAmount = totalAmount,
            TotalCount = totalCount,
            ByCategory = byCategory
        };
    }

    private async Task<IReadOnlyList<Tag>> ResolveTagsAsync(
        Guid userId, IReadOnlyCollection<Guid>? tagIds, CancellationToken cancellationToken)
    {
        var ids = (tagIds ?? Array.Empty<Guid>()).Distinct().ToArray();
        if (ids.Length > 10)
            throw new DomainException("EXPENSE_TAG_LIMIT", "Un gasto puede tener como maximo 10 etiquetas");
        if (ids.Length == 0)
            return Array.Empty<Tag>();

        var tags = await _tagRepository.GetActiveByIdsAsync(userId, ids, cancellationToken);
        if (tags.Count != ids.Length)
            throw new DomainException("INVALID_EXPENSE_TAGS", "Una o mas etiquetas no existen o no pertenecen al usuario");
        foreach (var tag in tags)
            tag.LastUsedAt = DateTimeOffset.UtcNow;
        return tags;
    }

    /// <summary>
    /// Convierte PascalCase a snake_case.
    /// DebitCard   -> debit_card
    /// CreditCard  -> credit_card
    /// Monthly     -> monthly
    /// </summary>
    private static string ToSnakeCase(string value)
    {
        return string.Concat(value.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? "_" + c : c.ToString()
        )).ToLower();
    }

    private static ExpenseResponseDto MapToResponseDto(Expense expense) => new()
    {
        Id = expense.Id,
        CategoryId = expense.CategoryId,
        AccountId = expense.AccountId,
        AccountName = expense.Account?.Name,
        CategoryName = expense.Category?.Name ?? string.Empty,
        CategoryColor = expense.Category?.Color ?? string.Empty,
        CategoryIcon = expense.Category?.Icon,
        Amount = expense.Amount,
        Description = expense.Description,
        Merchant = expense.Merchant,
        Tags = expense.ExpenseTags
            .Where(et => et.Tag.DeletedAt == null)
            .Select(et => new TagResponseDto
            {
                Id = et.Tag.Id,
                Name = et.Tag.Name,
                Color = et.Tag.Color,
                LastUsedAt = et.Tag.LastUsedAt
            }).OrderBy(t => t.Name).ToList(),
        Date = expense.Date,
        // Convierte PascalCase a snake_case: DebitCard → debit_card
        PaymentMethod = ToSnakeCase(expense.PaymentMethod.ToString()),
        IsRecurring = expense.IsRecurring,
        RecurrenceType = expense.RecurrenceType.HasValue
            ? ToSnakeCase(expense.RecurrenceType.Value.ToString())
            : null,
        Notes = expense.Notes,
        CreatedAt = expense.CreatedAt
    };
}