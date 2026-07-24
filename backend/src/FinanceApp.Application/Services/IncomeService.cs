using FinanceApp.Application.DTOs.Common;
using FinanceApp.Application.DTOs.Income;
using FinanceApp.Application.Interfaces;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.Interfaces.Repositories;

namespace FinanceApp.Application.Services;

public class IncomeService : IIncomeService
{
    private readonly IIncomeRepository _incomeRepository;
    private readonly IFinancialAccountService _accountService;
    private readonly IUserRepository _userRepository;

    public IncomeService(
        IIncomeRepository incomeRepository,
        IFinancialAccountService accountService,
        IUserRepository userRepository)
    {
        _incomeRepository = incomeRepository;
        _accountService = accountService;
        _userRepository = userRepository;
    }

    public async Task<PagedResponse<IncomeResponseDto>> GetAllAsync(
        Guid userId, IncomeFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _incomeRepository.GetByUserIdAsync(
            userId, filter.Page, filter.PageSize, filter.CategoryId,
            filter.StartDate, filter.EndDate, cancellationToken);

        return new PagedResponse<IncomeResponseDto>
        {
            Items = items.Select(MapToResponseDto),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<IncomeResponseDto> GetByIdAsync(
        Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var income = await _incomeRepository.GetByIdAsync(id, cancellationToken);
        if (income == null || income.UserId != userId || income.IsDeleted)
            throw new NotFoundException("Ingreso", id);
        return MapToResponseDto(income);
    }

    public async Task<IncomeResponseDto> CreateAsync(
        Guid userId, IncomeCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        var assignedCycleStart = await ResolveAssignedCycleStartAsync(
            userId, dto.Date, dto.AssignedCycleStart, cancellationToken);
        var income = new Income
        {
            UserId = userId,
            CategoryId = dto.CategoryId,
            AccountId = dto.AccountId,
            Amount = dto.Amount,
            Description = dto.Description?.Trim(),
            Date = dto.Date,
            AssignedCycleStart = assignedCycleStart,
            Source = dto.Source?.Trim()
        };

        await _incomeRepository.CreateAsync(income, cancellationToken);
        await _accountService.SyncMovementAsync(
            userId, dto.AccountId, FinancialAccountType.Cash, dto.Amount,
            dto.Date, "income", income.Id,
            dto.Description?.Trim() ?? dto.Source?.Trim() ?? "Ingreso",
            cancellationToken);

        return await GetByIdAsync(income.Id, userId, cancellationToken);
    }

    public async Task<IncomeResponseDto> UpdateAsync(
        Guid id, Guid userId, IncomeUpdateDto dto,
        CancellationToken cancellationToken = default)
    {
        var income = await _incomeRepository.GetByIdAsync(id, cancellationToken);
        if (income == null || income.UserId != userId || income.IsDeleted)
            throw new NotFoundException("Ingreso", id);

        income.CategoryId = dto.CategoryId;
        income.AccountId = dto.AccountId;
        income.Amount = dto.Amount;
        income.Description = dto.Description?.Trim();
        income.Date = dto.Date;
        income.AssignedCycleStart = await ResolveAssignedCycleStartAsync(
            userId, dto.Date, dto.AssignedCycleStart, cancellationToken);
        income.Source = dto.Source?.Trim();

        await _incomeRepository.UpdateAsync(income, cancellationToken);
        await _accountService.SyncMovementAsync(
            userId, dto.AccountId, FinancialAccountType.Cash, dto.Amount,
            dto.Date, "income", income.Id,
            dto.Description?.Trim() ?? dto.Source?.Trim() ?? "Ingreso",
            cancellationToken);

        return await GetByIdAsync(income.Id, userId, cancellationToken);
    }

    public async Task DeleteAsync(
        Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var income = await _incomeRepository.GetByIdAsync(id, cancellationToken);
        if (income == null || income.UserId != userId || income.IsDeleted)
            throw new NotFoundException("Ingreso", id);

        income.DeletedAt = DateTimeOffset.UtcNow;
        await _incomeRepository.UpdateAsync(income, cancellationToken);
        await _accountService.SyncMovementAsync(
            userId, income.AccountId, FinancialAccountType.Cash, 0,
            income.Date, "income", income.Id, "Ingreso eliminado",
            cancellationToken);
    }

    public async Task<IncomeSummaryDto> GetSummaryAsync(
        Guid userId, int month, int year,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _incomeRepository.GetByUserIdAsync(
            userId, 1, int.MaxValue,
            startDate: new DateOnly(year, month, 1),
            endDate: new DateOnly(year, month, DateTime.DaysInMonth(year, month)),
            cancellationToken: cancellationToken);

        var incomeList = items.ToList();
        var totalAmount = incomeList.Sum(i => i.Amount);
        var byCategory = incomeList
            .GroupBy(i => new { i.CategoryId, i.Category.Name, i.Category.Color })
            .Select(g => new IncomeByCategoryDto
            {
                CategoryName = g.Key.Name,
                CategoryColor = g.Key.Color,
                Amount = g.Sum(i => i.Amount),
                Percentage = totalAmount > 0
                    ? Math.Round(g.Sum(i => i.Amount) * 100 / totalAmount, 2)
                    : 0
            })
            .OrderByDescending(x => x.Amount)
            .ToList();

        return new IncomeSummaryDto
        {
            TotalAmount = totalAmount,
            TotalCount = totalCount,
            ByCategory = byCategory
        };
    }

    private async Task<DateOnly?> ResolveAssignedCycleStartAsync(
        Guid userId, DateOnly incomeDate, DateOnly? requested,
        CancellationToken cancellationToken)
    {
        if (requested.HasValue) return requested;
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user?.PaydayDay is null) return null;

        var payday = Math.Min(
            user.PaydayDay.Value,
            DateTime.DaysInMonth(incomeDate.Year, incomeDate.Month));
        var upcomingStart = new DateOnly(incomeDate.Year, incomeDate.Month, payday);
        var daysBeforePayday = upcomingStart.DayNumber - incomeDate.DayNumber;
        return daysBeforePayday is >= 0 and <= 7 ? upcomingStart : null;
    }

    private static IncomeResponseDto MapToResponseDto(Income income) => new()
    {
        Id = income.Id,
        CategoryId = income.CategoryId,
        CategoryName = income.Category?.Name ?? string.Empty,
        CategoryColor = income.Category?.Color ?? string.Empty,
        CategoryIcon = income.Category?.Icon,
        AccountId = income.AccountId,
        AccountName = income.Account?.Name,
        Amount = income.Amount,
        Description = income.Description,
        Date = income.Date,
        AssignedCycleStart = income.AssignedCycleStart,
        Source = income.Source,
        CreatedAt = income.CreatedAt
    };
}
