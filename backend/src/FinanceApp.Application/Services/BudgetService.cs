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

    private static readonly string[] MonthNames =
    {
        "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
        "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"
    };

    public BudgetService(
        IBudgetRepository budgetRepository,
        IExpenseRepository expenseRepository)
    {
        _budgetRepository = budgetRepository;
        _expenseRepository = expenseRepository;
    }

    public async Task<IEnumerable<BudgetResponseDto>> GetAllAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var budgets = await _budgetRepository.GetByUserIdAsync(userId, cancellationToken);
        return budgets.Select(MapToResponseDto);
    }

    public async Task<BudgetResponseDto?> GetCurrentAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        var budget = await _budgetRepository.GetByUserAndPeriodAsync(
            userId, today.Month, today.Year, cancellationToken);

        return budget != null ? MapToResponseDto(budget) : null;
    }

    public async Task<BudgetResponseDto?> GetByPeriodAsync(
        Guid userId,
        int month,
        int year,
        CancellationToken cancellationToken = default)
    {
        var budget = await _budgetRepository.GetByUserAndPeriodAsync(
            userId, month, year, cancellationToken);

        return budget != null ? MapToResponseDto(budget) : null;
    }

    public async Task<BudgetResponseDto> CreateAsync(
        Guid userId,
        BudgetCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        // Verificar que no existe un presupuesto para ese mes/año
        var existing = await _budgetRepository.GetByUserAndPeriodAsync(
            userId, dto.Month, dto.Year, cancellationToken);

        if (existing != null)
            throw new DomainException(
                "BUDGET_ALREADY_EXISTS",
                $"Ya existe un presupuesto para {MonthNames[dto.Month - 1]} {dto.Year}");

        var budget = new BudgetPeriod
        {
            UserId = userId,
            Month = dto.Month,
            Year = dto.Year,
            TotalLimit = dto.TotalLimit,
            Notes = dto.Notes?.Trim(),
            BudgetCategories = dto.Categories.Select(c => new BudgetCategory
            {
                CategoryId = c.CategoryId,
                AmountLimit = c.AmountLimit
            }).ToList()
        };

        await _budgetRepository.CreateAsync(budget, cancellationToken);

        // Recargamos con las relaciones incluidas
        var created = await _budgetRepository.GetByIdAsync(budget.Id, cancellationToken);
        return MapToResponseDto(created!);
    }

    public async Task<BudgetResponseDto> UpdateAsync(
        Guid id,
        Guid userId,
        BudgetUpdateDto dto,
        CancellationToken cancellationToken = default)
    {
        var budget = await _budgetRepository.GetByIdAsync(id, cancellationToken);

        if (budget == null || budget.UserId != userId)
            throw new NotFoundException("Presupuesto", id);

        budget.TotalLimit = dto.TotalLimit;
        budget.Notes = dto.Notes?.Trim();

        // Reemplazamos todas las categorías del presupuesto
        budget.BudgetCategories = dto.Categories.Select(c => new BudgetCategory
        {
            BudgetPeriodId = budget.Id,
            CategoryId = c.CategoryId,
            AmountLimit = c.AmountLimit
        }).ToList();

        await _budgetRepository.UpdateAsync(budget, cancellationToken);

        var updated = await _budgetRepository.GetByIdAsync(budget.Id, cancellationToken);
        return MapToResponseDto(updated!);
    }

    public async Task<BudgetStatusDto> GetStatusAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var budget = await _budgetRepository.GetByIdAsync(id, cancellationToken);

        if (budget == null || budget.UserId != userId)
            throw new NotFoundException("Presupuesto", id);

        // Calculamos el gasto real por categoría en el período del presupuesto
        var categoryStatuses = new List<BudgetCategoryStatusDto>();

        foreach (var bc in budget.BudgetCategories)
        {
            // Gastos reales de esta categoría en el mes del presupuesto
            var (items, _) = await _expenseRepository.GetByUserIdAsync(
                userId,
                page: 1,
                pageSize: int.MaxValue,
                categoryId: bc.CategoryId,
                startDate: new DateOnly(budget.Year, budget.Month, 1),
                endDate: new DateOnly(budget.Year, budget.Month,
                    DateTime.DaysInMonth(budget.Year, budget.Month)),
                cancellationToken: cancellationToken);

            var amountSpent = items.Sum(e => e.Amount);
            var amountRemaining = bc.AmountLimit - amountSpent;
            var percentageUsed = bc.AmountLimit > 0
                ? Math.Round(amountSpent / bc.AmountLimit * 100, 2)
                : 0;

            categoryStatuses.Add(new BudgetCategoryStatusDto
            {
                CategoryName = bc.Category.Name,
                CategoryColor = bc.Category.Color,
                CategoryIcon = bc.Category.Icon,
                AmountLimit = bc.AmountLimit,
                AmountSpent = amountSpent,
                AmountRemaining = amountRemaining,
                PercentageUsed = percentageUsed,
                IsOverBudget = amountSpent > bc.AmountLimit
            });
        }

        var totalLimit = budget.TotalLimit
            ?? budget.BudgetCategories.Sum(bc => bc.AmountLimit);
        var totalSpent = categoryStatuses.Sum(c => c.AmountSpent);
        var totalRemaining = totalLimit - totalSpent;
        var totalPercentage = totalLimit > 0
            ? Math.Round(totalSpent / totalLimit * 100, 2)
            : 0;

        return new BudgetStatusDto
        {
            Period = $"{MonthNames[budget.Month - 1]} {budget.Year}",
            TotalLimit = totalLimit,
            TotalSpent = totalSpent,
            TotalRemaining = totalRemaining,
            PercentageUsed = totalPercentage,
            IsOverBudget = totalSpent > totalLimit,
            Categories = categoryStatuses
        };
    }

    private static BudgetResponseDto MapToResponseDto(BudgetPeriod budget) => new()
    {
        Id = budget.Id,
        Month = budget.Month,
        Year = budget.Year,
        Period = $"{MonthNames[budget.Month - 1]} {budget.Year}",
        TotalLimit = budget.TotalLimit,
        Notes = budget.Notes,
        Categories = budget.BudgetCategories.Select(bc => new BudgetCategoryResponseDto
        {
            Id = bc.Id,
            CategoryId = bc.CategoryId,
            CategoryName = bc.Category?.Name ?? string.Empty,
            CategoryColor = bc.Category?.Color ?? string.Empty,
            CategoryIcon = bc.Category?.Icon,
            AmountLimit = bc.AmountLimit
        }).ToList()
    };
}