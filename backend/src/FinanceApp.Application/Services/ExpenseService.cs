using FinanceApp.Application.DTOs.Common;
using FinanceApp.Application.DTOs.Expense;
using FinanceApp.Application.Interfaces;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.Interfaces.Repositories;

namespace FinanceApp.Application.Services;

public class ExpenseService : IExpenseService
{
    private readonly IExpenseRepository _expenseRepository;
    private readonly IFinancialAccountService _accountService;

    public ExpenseService(IExpenseRepository expenseRepository, IFinancialAccountService accountService)
    {
        _expenseRepository = expenseRepository;
        _accountService = accountService;
    }

    public async Task<PagedResponse<ExpenseResponseDto>> GetAllAsync(
        Guid userId,
        ExpenseFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _expenseRepository.GetByUserIdAsync(
            userId,
            filter.Page,
            filter.PageSize,
            filter.CategoryId,
            filter.StartDate,
            filter.EndDate,
            cancellationToken);

        return new PagedResponse<ExpenseResponseDto>
        {
            Items = items.Select(MapToResponseDto),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
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
        var expense = await _expenseRepository.GetByIdAsync(id, cancellationToken);

        if (expense == null || expense.UserId != userId || expense.IsDeleted)
            throw new NotFoundException("Gasto", id);

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

    /// <summary>
    /// Convierte PascalCase a snake_case.
    /// DebitCard   → debit_card
    /// CreditCard  → credit_card
    /// Monthly     → monthly
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