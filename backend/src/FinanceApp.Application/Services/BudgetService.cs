using FinanceApp.Application.DTOs.Budget;
using FinanceApp.Application.Interfaces;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.Interfaces.Repositories;

namespace FinanceApp.Application.Services;

public class BudgetService : IBudgetService
{
    private readonly IBudgetRepository _budgetRepository;
    private readonly IExpenseRepository _expenseRepository;
    private readonly IReimbursementRepository _reimbursementRepository;
    private readonly IBusinessDateProvider _businessDateProvider;
    private readonly IUserRepository _userRepository;

    private static readonly string[] MonthNames =
    {
        "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
        "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"
    };

    public BudgetService(
        IBudgetRepository budgetRepository,
        IExpenseRepository expenseRepository,
        IReimbursementRepository reimbursementRepository,
        IUserRepository userRepository,
        IBusinessDateProvider businessDateProvider)
    {
        _budgetRepository = budgetRepository;
        _expenseRepository = expenseRepository;
        _reimbursementRepository = reimbursementRepository;
        _userRepository = userRepository;
        _businessDateProvider = businessDateProvider;
    }

    public async Task<IEnumerable<BudgetResponseDto>> GetAllAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        var budgets = await _budgetRepository.GetByUserIdAsync(
            userId, cancellationToken);
        return budgets.Select(MapToResponseDto);
    }

    public async Task<BudgetResponseDto?> GetCurrentAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        var today = _businessDateProvider.Today;
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        var (month, year) = GetCurrentCycle(today, user?.PaydayDay);
        return await GetByPeriodAsync(userId, month, year, cancellationToken);
    }

    public async Task<BudgetResponseDto?> GetByPeriodAsync(
        Guid userId, int month, int year,
        CancellationToken cancellationToken = default)
    {
        var budget = await _budgetRepository.GetByUserAndPeriodAsync(
            userId, month, year, cancellationToken);

        // El último presupuesto funciona como plantilla viva. Los cambios
        // mensuales afectan solo al nuevo período que se generó.
        if (budget == null)
        {
            var previous = (await _budgetRepository.GetByUserIdAsync(
                userId, cancellationToken)).FirstOrDefault();
            if (previous != null)
            {
                budget = new BudgetPeriod
                {
                    UserId = userId,
                    Month = month,
                    Year = year,
                    TotalLimit = previous.TotalLimit,
                    Notes = previous.Notes,
                    BudgetCategories = previous.BudgetCategories.Select(c =>
                        new BudgetCategory
                        {
                            CategoryId = c.CategoryId,
                            AmountLimit = c.AmountLimit
                        }).ToList()
                };
                await _budgetRepository.CreateAsync(budget, cancellationToken);
                budget = await _budgetRepository.GetByIdAsync(
                    budget.Id, cancellationToken);
            }
        }

        return budget != null ? MapToResponseDto(budget) : null;
    }

    public async Task<BudgetResponseDto> CreateAsync(
        Guid userId, BudgetCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        var existing = await _budgetRepository.GetByUserAndPeriodAsync(
            userId, dto.Month, dto.Year, cancellationToken);
        if (existing != null)
        {
            if (dto.BudgetRolloverIdempotencyKey.HasValue
                && dto.BudgetRolloverIdempotencyKey == existing.BudgetRolloverIdempotencyKey)
                return MapToResponseDto(existing);
            throw new DomainException(
                "BUDGET_ALREADY_EXISTS",
                $"Ya existe un presupuesto para {MonthNames[dto.Month - 1]} {dto.Year}");
        }

        if (dto.BudgetRolloverAmount != 0 && !dto.BudgetRolloverIdempotencyKey.HasValue)
            throw new DomainException(
                "INVALID_ROLLOVER_IDEMPOTENCY_KEY",
                "El rollover inicial requiere una clave de idempotencia.");

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        var (cycleStart, cycleEnd) = GetCycleRange(dto.Month, dto.Year, user?.PaydayDay);
        DateOnly? rolloverDate = dto.BudgetRolloverAmount == 0
            ? null
            : dto.BudgetRolloverDate ?? _businessDateProvider.Today;
        if (rolloverDate.HasValue && (rolloverDate < cycleStart || rolloverDate > cycleEnd))
            throw new DomainException(
                "INVALID_ROLLOVER_DATE",
                "La fecha del rollover debe pertenecer al ciclo del presupuesto.");

        var budget = new BudgetPeriod
        {
            UserId = userId,
            Month = dto.Month,
            Year = dto.Year,
            TotalLimit = dto.TotalLimit,
            Notes = dto.Notes?.Trim(),
            BudgetRolloverAmount = dto.BudgetRolloverAmount,
            BudgetRolloverDate = rolloverDate,
            BudgetRolloverNote = dto.BudgetRolloverNote?.Trim(),
            BudgetRolloverIdempotencyKey = dto.BudgetRolloverIdempotencyKey,
            BudgetCategories = dto.Categories.Select(c => new BudgetCategory
            {
                CategoryId = c.CategoryId,
                AmountLimit = c.AmountLimit
            }).ToList()
        };

        await _budgetRepository.CreateAsync(budget, cancellationToken);
        var created = await _budgetRepository.GetByIdAsync(
            budget.Id, cancellationToken);
        return MapToResponseDto(created!);
    }

    public async Task<BudgetResponseDto> UpdateAsync(
        Guid id, Guid userId, BudgetUpdateDto dto,
        CancellationToken cancellationToken = default)
    {
        var budget = await _budgetRepository.GetByIdAsync(id, cancellationToken);
        if (budget == null || budget.UserId != userId)
            throw new NotFoundException("Presupuesto", id);

        budget.TotalLimit = dto.TotalLimit;
        budget.Notes = dto.Notes?.Trim();
        budget.BudgetCategories = dto.Categories.Select(c => new BudgetCategory
        {
            BudgetPeriodId = budget.Id,
            CategoryId = c.CategoryId,
            AmountLimit = c.AmountLimit
        }).ToList();

        await _budgetRepository.UpdateAsync(budget, cancellationToken);
        var updated = await _budgetRepository.GetByIdAsync(
            budget.Id, cancellationToken);
        return MapToResponseDto(updated!);
    }

    public async Task<BudgetStatusDto> GetStatusAsync(
        Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var budget = await _budgetRepository.GetByIdAsync(id, cancellationToken);
        if (budget == null || budget.UserId != userId)
            throw new NotFoundException("Presupuesto", id);

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        var (start, end) = GetCycleRange(
            budget.Month, budget.Year, user?.PaydayDay);
        var categoryStatuses = new List<BudgetCategoryStatusDto>();
        // Se acreditan únicamente los reembolsos recibidos en este ciclo y vinculados a un gasto.
        // Así un ciclo cerrado no se reescribe cuando el reembolso llega después.
        var reimbursementsByCategory = (await _reimbursementRepository.GetByUserIdAsync(
            userId, start, end, cancellationToken))
            .Where(r => r.Expense is not null)
            .GroupBy(r => r.Expense!.CategoryId)
            .ToDictionary(group => group.Key, group => group.Sum(r => r.Amount));

        foreach (var category in budget.BudgetCategories)
        {
            var (items, _) = await _expenseRepository.GetByUserIdAsync(
                userId, 1, int.MaxValue, category.CategoryId,
                start, end, cancellationToken);
            var grossAmountSpent = items.Sum(e => e.Amount);
            var reimbursementAmount = reimbursementsByCategory.GetValueOrDefault(category.CategoryId);
            var amountSpent = grossAmountSpent - reimbursementAmount;
            var percentageUsed = category.AmountLimit > 0
                ? Math.Round(amountSpent / category.AmountLimit * 100, 2)
                : 0;

            categoryStatuses.Add(new BudgetCategoryStatusDto
            {
                CategoryName = category.Category.Name,
                CategoryColor = category.Category.Color,
                CategoryIcon = category.Category.Icon,
                AmountLimit = category.AmountLimit,
                AmountSpent = amountSpent,
                AmountRemaining = category.AmountLimit - amountSpent,
                PercentageUsed = percentageUsed,
                IsOverBudget = amountSpent > category.AmountLimit
            });
        }

        var totalLimit = budget.TotalLimit
            ?? budget.BudgetCategories.Sum(c => c.AmountLimit);
        var totalSpent = categoryStatuses.Sum(c => c.AmountSpent);

        return new BudgetStatusDto
        {
            Period = $"{start:dd MMM} – {end:dd MMM}",
            TotalLimit = totalLimit,
            BudgetRolloverAmount = budget.BudgetRolloverAmount,
            TotalSpent = totalSpent,
            TotalRemaining = totalLimit + budget.BudgetRolloverAmount - totalSpent,
            PercentageUsed = totalLimit + budget.BudgetRolloverAmount > 0
                ? Math.Round(totalSpent / (totalLimit + budget.BudgetRolloverAmount) * 100, 2)
                : 0,
            IsOverBudget = totalSpent > totalLimit + budget.BudgetRolloverAmount,
            Categories = categoryStatuses
        };
    }

    private static (int Month, int Year) GetCurrentCycle(
        DateOnly today, int? paydayDay)
    {
        if (paydayDay is null) return (today.Month, today.Year);
        var day = Math.Min(
            paydayDay.Value,
            DateTime.DaysInMonth(today.Year, today.Month));
        if (today.Day >= day) return (today.Month, today.Year);
        var previous = today.AddMonths(-1);
        return (previous.Month, previous.Year);
    }

    private static (DateOnly Start, DateOnly End) GetCycleRange(
        int month, int year, int? paydayDay)
    {
        if (paydayDay is null)
            return (
                new DateOnly(year, month, 1),
                new DateOnly(year, month, DateTime.DaysInMonth(year, month)));

        var day = Math.Min(paydayDay.Value, DateTime.DaysInMonth(year, month));
        var start = new DateOnly(year, month, day);
        var next = start.AddMonths(1);
        var nextDay = Math.Min(
            paydayDay.Value,
            DateTime.DaysInMonth(next.Year, next.Month));
        return (start, new DateOnly(next.Year, next.Month, nextDay).AddDays(-1));
    }

    private static BudgetResponseDto MapToResponseDto(BudgetPeriod budget) => new()
    {
        Id = budget.Id,
        Month = budget.Month,
        Year = budget.Year,
        Period = $"{MonthNames[budget.Month - 1]} {budget.Year}",
        TotalLimit = budget.TotalLimit,
        Notes = budget.Notes,
        BudgetRolloverAmount = budget.BudgetRolloverAmount,
        BudgetRolloverDate = budget.BudgetRolloverDate,
        BudgetRolloverNote = budget.BudgetRolloverNote,
        Categories = budget.BudgetCategories.Select(c =>
            new BudgetCategoryResponseDto
            {
                Id = c.Id,
                CategoryId = c.CategoryId,
                CategoryName = c.Category?.Name ?? string.Empty,
                CategoryColor = c.Category?.Color ?? string.Empty,
                CategoryIcon = c.Category?.Icon,
                AmountLimit = c.AmountLimit
            }).ToList()
    };
}
