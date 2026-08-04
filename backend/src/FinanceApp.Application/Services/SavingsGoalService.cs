using FinanceApp.Application.DTOs.SavingsGoal;
using FinanceApp.Application.Interfaces;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.Interfaces.Repositories;

namespace FinanceApp.Application.Services;

public class SavingsGoalService : ISavingsGoalService
{
    private readonly ISavingsGoalRepository _savingsGoalRepository;
    private readonly IFinancialAccountService _accountService;

    public SavingsGoalService(
        ISavingsGoalRepository savingsGoalRepository,
        IFinancialAccountService accountService)
    {
        _savingsGoalRepository = savingsGoalRepository;
        _accountService = accountService;
    }

    public async Task<IEnumerable<SavingsGoalResponseDto>> GetAllAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        var goals = await _savingsGoalRepository.GetByUserIdAsync(
            userId, cancellationToken);
        return goals.Select(MapToResponseDto);
    }

    public async Task<SavingsGoalResponseDto> GetByIdAsync(
        Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var goal = await _savingsGoalRepository.GetByIdWithHistoryAsync(id, userId, cancellationToken);
        if (goal == null || goal.UserId != userId || goal.IsDeleted)
            throw new NotFoundException("Meta de ahorro", id);
        return MapToResponseDto(goal);
    }

    public async Task<SavingsGoalResponseDto> CreateAsync(
        Guid userId, SavingsGoalCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<SavingsGoalPurpose>(dto.Purpose.Replace("_", ""), true, out var purpose))
            throw new DomainException("INVALID_GOAL_PURPOSE", "El prop?sito de la meta no es v?lido.");
        if (dto.MinimumProtectedAmount is < 0 || dto.MinimumProtectedAmount > dto.TargetAmount)
            throw new DomainException("INVALID_PROTECTED_AMOUNT", "El m?nimo protegido debe estar entre cero y el objetivo.");
        if (purpose == SavingsGoalPurpose.EmergencyFund)
        {
            var goals = await _savingsGoalRepository.GetByUserIdAsync(userId, cancellationToken);
            if (goals.Any(g => g.Purpose == SavingsGoalPurpose.EmergencyFund))
                throw new DomainException("EMERGENCY_FUND_ALREADY_EXISTS", "Solo puede existir un fondo de emergencia activo.");
        }


        await _accountService.GetOrCreateDefaultAsync(
            userId, FinancialAccountType.Savings, cancellationToken);

        var goal = new SavingsGoal
        {
            UserId = userId,
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            TargetAmount = dto.TargetAmount,
            CurrentAmount = dto.InitialAmount,
            TargetDate = dto.TargetDate,
            Icon = dto.Icon?.Trim(),
            Purpose = purpose,
            MinimumProtectedAmount = purpose == SavingsGoalPurpose.EmergencyFund ? dto.MinimumProtectedAmount : null,
            IsCompleted = dto.InitialAmount >= dto.TargetAmount
        };

        await _savingsGoalRepository.CreateAsync(goal, cancellationToken);
        if (dto.InitialAmount != 0)
        {
            await _accountService.SyncMovementAsync(
                userId, null, FinancialAccountType.Savings, dto.InitialAmount,
                DateOnly.FromDateTime(DateTime.Today), "savings-opening", goal.Id,
                $"Saldo inicial: {goal.Name}", cancellationToken);
        }

        return MapToResponseDto(goal);
    }

    public async Task<SavingsGoalResponseDto> UpdateAsync(
        Guid id, Guid userId, SavingsGoalUpdateDto dto,
        CancellationToken cancellationToken = default)
    {
        var goal = await _savingsGoalRepository.GetByIdWithHistoryAsync(id, userId, cancellationToken);
        if (goal == null || goal.UserId != userId || goal.IsDeleted)
            throw new NotFoundException("Meta de ahorro", id);

        if (!Enum.TryParse<SavingsGoalPurpose>(dto.Purpose.Replace("_", ""), true, out var purpose))
            throw new DomainException("INVALID_GOAL_PURPOSE", "El prop?sito de la meta no es v?lido.");
        if (dto.MinimumProtectedAmount is < 0 || dto.MinimumProtectedAmount > dto.TargetAmount)
            throw new DomainException("INVALID_PROTECTED_AMOUNT", "El m?nimo protegido debe estar entre cero y el objetivo.");
        if (purpose == SavingsGoalPurpose.EmergencyFund && goal.Purpose != SavingsGoalPurpose.EmergencyFund)
        {
            var goals = await _savingsGoalRepository.GetByUserIdAsync(userId, cancellationToken);
            if (goals.Any(g => g.Id != id && g.Purpose == SavingsGoalPurpose.EmergencyFund))
                throw new DomainException("EMERGENCY_FUND_ALREADY_EXISTS", "Solo puede existir un fondo de emergencia activo.");
        }
        if (goal.Purpose == SavingsGoalPurpose.EmergencyFund && purpose != SavingsGoalPurpose.EmergencyFund
            && goal.Restorations.Any(r => r.Status == EmergencyFundRestorationStatus.Open && !r.IsDeleted))
            throw new DomainException("OPEN_RESTORATIONS", "No se puede cambiar el prop?sito mientras existan restauraciones abiertas.");

        goal.Name = dto.Name.Trim();
        goal.Description = dto.Description?.Trim();
        goal.TargetAmount = dto.TargetAmount;
        goal.TargetDate = dto.TargetDate;
        goal.Icon = dto.Icon?.Trim();
        goal.IsCompleted = goal.CurrentAmount >= goal.TargetAmount;
        goal.Purpose = purpose;
        goal.MinimumProtectedAmount = purpose == SavingsGoalPurpose.EmergencyFund ? dto.MinimumProtectedAmount : null;

        await _savingsGoalRepository.UpdateAsync(goal, cancellationToken);
        return MapToResponseDto(goal);
    }

    public async Task DeleteAsync(
        Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var goal = await _savingsGoalRepository.GetByIdWithHistoryAsync(id, userId, cancellationToken);
        if (goal == null || goal.UserId != userId || goal.IsDeleted)
            throw new NotFoundException("Meta de ahorro", id);

        // El dinero permanece en el fondo de ahorro aunque se elimine
        // su etiqueta virtual.
        if (goal.Restorations.Any(r => r.Status == EmergencyFundRestorationStatus.Open && !r.IsDeleted))
            throw new DomainException("OPEN_RESTORATIONS", "No se puede archivar un fondo con restauraciones abiertas.");

        goal.DeletedAt = DateTimeOffset.UtcNow;
        await _savingsGoalRepository.UpdateAsync(goal, cancellationToken);
    }

    public async Task<SavingsGoalResponseDto> DepositAsync(
        Guid id, Guid userId, DepositDto dto,
        CancellationToken cancellationToken = default)
    {
        var goal = await _savingsGoalRepository.GetByIdWithHistoryAsync(id, userId, cancellationToken);
        if (goal == null || goal.UserId != userId || goal.IsDeleted)
            throw new NotFoundException("Meta de ahorro", id);
        if (goal.Restorations.Any(r => r.Status == EmergencyFundRestorationStatus.Open && !r.IsDeleted))
            throw new DomainException("USE_RESTORATION_PAYMENT", "Registra el aporte contra una restauraci?n abierta.");

        if (goal.IsCompleted)
            throw new DomainException(
                "GOAL_ALREADY_COMPLETED", "Esta meta de ahorro ya fue completada");
        if (dto.Amount <= 0)
            throw new DomainException(
                "INVALID_DEPOSIT_AMOUNT", "El monto del depósito debe ser mayor a 0");

        await _accountService.GetOrCreateDefaultAsync(
            userId, FinancialAccountType.Cash, cancellationToken);
        await _accountService.GetOrCreateDefaultAsync(
            userId, FinancialAccountType.Savings, cancellationToken);

        var appliedAmount = Math.Min(dto.Amount, goal.TargetAmount - goal.CurrentAmount);
        goal.CurrentAmount += appliedAmount;
        if (goal.CurrentAmount >= goal.TargetAmount)
        {
            goal.CurrentAmount = goal.TargetAmount;
            goal.IsCompleted = true;
        }

        var contribution = new SavingsGoalContribution
        {
            SavingsGoalId = goal.Id,
            ContributionDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Amount = appliedAmount,
            Notes = dto.Notes
        };
        await _savingsGoalRepository.AddContributionAsync(
            contribution, cancellationToken);
        await _accountService.SyncTransferAsync(
            userId, FinancialAccountType.Cash, FinancialAccountType.Savings,
            appliedAmount, contribution.ContributionDate, "savings-contribution",
            contribution.Id, $"Aporte: {goal.Name}", cancellationToken);

        return MapToResponseDto(goal);
    }

    public async Task<SavingsGoalWithdrawalResponseDto> WithdrawAsync(
        Guid id, Guid userId, SavingsGoalWithdrawalCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        var goal = await _savingsGoalRepository.GetByIdAsync(id, cancellationToken);
        if (goal == null || goal.UserId != userId || goal.IsDeleted)
            throw new NotFoundException("Meta de ahorro", id);
        if (dto.Amount <= 0)
            throw new DomainException(
                "INVALID_WITHDRAWAL_AMOUNT", "El monto del retiro debe ser mayor a 0");
        if (dto.Amount > goal.CurrentAmount)
            throw new DomainException(
                "INSUFFICIENT_SAVINGS_BALANCE",
                "El retiro no puede superar el saldo disponible");
        if (dto.LinkedExpenseId.HasValue
            && dto.Reason != SavingsWithdrawalReason.Consumed)
            throw new DomainException(
                "INVALID_LINKED_EXPENSE",
                "LinkedExpenseId solo es válido cuando Reason es Consumed");

        await _accountService.GetOrCreateDefaultAsync(
            userId, FinancialAccountType.Cash, cancellationToken);
        await _accountService.GetOrCreateDefaultAsync(
            userId, FinancialAccountType.Savings, cancellationToken);

        goal.CurrentAmount -= dto.Amount;
        goal.IsCompleted = goal.CurrentAmount >= goal.TargetAmount
            && goal.TargetAmount > 0;

        var withdrawal = new SavingsGoalWithdrawal
        {
            SavingsGoalId = goal.Id,
            WithdrawalDate = dto.WithdrawalDate
                ?? DateOnly.FromDateTime(DateTime.UtcNow),
            Amount = dto.Amount,
            LinkedExpenseId = dto.Reason == SavingsWithdrawalReason.Consumed
                ? dto.LinkedExpenseId
                : null,
            Reason = dto.Reason,
            Notes = dto.Notes
        };
        await _savingsGoalRepository.AddWithdrawalAsync(
            withdrawal, cancellationToken);

        if (dto.Reason == SavingsWithdrawalReason.Correction)
        {
            await _accountService.SyncMovementAsync(
                userId, null, FinancialAccountType.Savings, -dto.Amount,
                withdrawal.WithdrawalDate, "savings-correction", withdrawal.Id,
                $"Corrección: {goal.Name}", cancellationToken);
        }
        else
        {
            await _accountService.SyncTransferAsync(
                userId, FinancialAccountType.Savings, FinancialAccountType.Cash,
                dto.Amount, withdrawal.WithdrawalDate, "savings-withdrawal",
                withdrawal.Id, $"Retiro: {goal.Name}", cancellationToken);
        }

        return new SavingsGoalWithdrawalResponseDto
        {
            Id = withdrawal.Id,
            WithdrawalDate = withdrawal.WithdrawalDate,
            Amount = withdrawal.Amount,
            LinkedExpenseId = withdrawal.LinkedExpenseId,
            Reason = withdrawal.Reason,
            Notes = withdrawal.Notes,
            CreatedAt = withdrawal.CreatedAt,
            GoalCurrentAmountAfter = goal.CurrentAmount
        };
    }

    private static SavingsGoalResponseDto MapToResponseDto(SavingsGoal goal) => new()
    {
        Id = goal.Id,
        Name = goal.Name,
        Description = goal.Description,
        TargetAmount = goal.TargetAmount,
        CurrentAmount = goal.CurrentAmount,
        RemainingAmount = goal.RemainingAmount,
        ProgressPercentage = goal.ProgressPercentage,
        TargetDate = goal.TargetDate,
        IsCompleted = goal.IsCompleted,
        Icon = goal.Icon,
        EstimatedMonthsToComplete = null,
        CreatedAt = goal.CreatedAt,
        Purpose = goal.Purpose == SavingsGoalPurpose.EmergencyFund ? "emergency_fund" : "general",
        MinimumProtectedAmount = goal.MinimumProtectedAmount,
        PendingRestorationAmount = goal.Restorations.Where(r => r.Status == EmergencyFundRestorationStatus.Open && !r.IsDeleted).Sum(r => r.OutstandingAmount),
        OpenRestorationsCount = goal.Restorations.Count(r => r.Status == EmergencyFundRestorationStatus.Open && !r.IsDeleted),
        NextRestorationDate = goal.Restorations.Where(r => r.Status == EmergencyFundRestorationStatus.Open && !r.IsDeleted).Select(r => (DateOnly?)r.NextScheduledDate).Min(),
    };
}
