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

    public SavingsGoalService(ISavingsGoalRepository savingsGoalRepository)
    {
        _savingsGoalRepository = savingsGoalRepository;
    }

    public async Task<IEnumerable<SavingsGoalResponseDto>> GetAllAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var goals = await _savingsGoalRepository
            .GetByUserIdAsync(userId, cancellationToken);
        return goals.Select(MapToResponseDto);
    }

    public async Task<SavingsGoalResponseDto> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var goal = await _savingsGoalRepository.GetByIdAsync(id, cancellationToken);

        if (goal == null || goal.UserId != userId || goal.IsDeleted)
            throw new NotFoundException("Meta de ahorro", id);

        return MapToResponseDto(goal);
    }

    public async Task<SavingsGoalResponseDto> CreateAsync(
        Guid userId,
        SavingsGoalCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        var goal = new SavingsGoal
        {
            UserId = userId,
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            TargetAmount = dto.TargetAmount,
            CurrentAmount = dto.InitialAmount,
            TargetDate = dto.TargetDate,
            Icon = dto.Icon?.Trim(),
            IsCompleted = dto.InitialAmount >= dto.TargetAmount
        };

        await _savingsGoalRepository.CreateAsync(goal, cancellationToken);
        return MapToResponseDto(goal);
    }

    public async Task<SavingsGoalResponseDto> UpdateAsync(
        Guid id,
        Guid userId,
        SavingsGoalUpdateDto dto,
        CancellationToken cancellationToken = default)
    {
        var goal = await _savingsGoalRepository.GetByIdAsync(id, cancellationToken);

        if (goal == null || goal.UserId != userId || goal.IsDeleted)
            throw new NotFoundException("Meta de ahorro", id);

        goal.Name = dto.Name.Trim();
        goal.Description = dto.Description?.Trim();
        goal.TargetAmount = dto.TargetAmount;
        goal.TargetDate = dto.TargetDate;
        goal.Icon = dto.Icon?.Trim();

        // Recalculamos si ya se completó con el nuevo monto objetivo
        goal.IsCompleted = goal.CurrentAmount >= goal.TargetAmount;

        await _savingsGoalRepository.UpdateAsync(goal, cancellationToken);
        return MapToResponseDto(goal);
    }

    public async Task DeleteAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var goal = await _savingsGoalRepository.GetByIdAsync(id, cancellationToken);

        if (goal == null || goal.UserId != userId || goal.IsDeleted)
            throw new NotFoundException("Meta de ahorro", id);

        goal.DeletedAt = DateTimeOffset.UtcNow;
        await _savingsGoalRepository.UpdateAsync(goal, cancellationToken);
    }

    public async Task<SavingsGoalResponseDto> DepositAsync(
        Guid id,
        Guid userId,
        DepositDto dto,
        CancellationToken cancellationToken = default)
    {
        var goal = await _savingsGoalRepository.GetByIdAsync(id, cancellationToken);

        if (goal == null || goal.UserId != userId || goal.IsDeleted)
            throw new NotFoundException("Meta de ahorro", id);

        if (goal.IsCompleted)
            throw new DomainException(
                "GOAL_ALREADY_COMPLETED",
                "Esta meta de ahorro ya fue completada");

        if (dto.Amount <= 0)
            throw new DomainException(
                "INVALID_DEPOSIT_AMOUNT",
                "El monto del depósito debe ser mayor a 0");

        goal.CurrentAmount += dto.Amount;

        // Verificamos si se completó la meta con este depósito
        if (goal.CurrentAmount >= goal.TargetAmount)
        {
            goal.CurrentAmount = goal.TargetAmount; // no superamos el objetivo
            goal.IsCompleted = true;
        }

        await _savingsGoalRepository.UpdateAsync(goal, cancellationToken);

        // v2.0.1 — registro histórico. El contrato del endpoint /deposit
        // no cambia; esto solo agrega trazabilidad, no reemplaza CurrentAmount.
        var contribution = new SavingsGoalContribution
        {
            SavingsGoalId = goal.Id,
            ContributionDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Amount = dto.Amount,
            Notes = dto.Notes
        };
        await _savingsGoalRepository.AddContributionAsync(contribution, cancellationToken);

        return MapToResponseDto(goal);
    }

    public async Task<SavingsGoalWithdrawalResponseDto> WithdrawAsync(
        Guid id,
        Guid userId,
        SavingsGoalWithdrawalCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        var goal = await _savingsGoalRepository.GetByIdAsync(id, cancellationToken);

        if (goal == null || goal.UserId != userId || goal.IsDeleted)
            throw new NotFoundException("Meta de ahorro", id);

        if (dto.Amount <= 0)
            throw new DomainException(
                "INVALID_WITHDRAWAL_AMOUNT",
                "El monto del retiro debe ser mayor a 0");

        // Regla del spec 3.2: LinkedExpenseId solo tiene sentido si Reason = Consumed
        if (dto.LinkedExpenseId.HasValue && dto.Reason != SavingsWithdrawalReason.Consumed)
            throw new DomainException(
                "INVALID_LINKED_EXPENSE",
                "LinkedExpenseId solo es válido cuando Reason es Consumed");

        goal.CurrentAmount = Math.Max(0, goal.CurrentAmount - dto.Amount);

        // Un retiro puede sacar la meta de "completada" si ya lo estaba
        goal.IsCompleted = goal.CurrentAmount >= goal.TargetAmount && goal.TargetAmount > 0;

        await _savingsGoalRepository.UpdateAsync(goal, cancellationToken);

        var withdrawal = new SavingsGoalWithdrawal
        {
            SavingsGoalId = goal.Id,
            WithdrawalDate = dto.WithdrawalDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            Amount = dto.Amount,
            LinkedExpenseId = dto.Reason == SavingsWithdrawalReason.Consumed
                ? dto.LinkedExpenseId
                : null,
            Reason = dto.Reason,
            Notes = dto.Notes
        };
        await _savingsGoalRepository.AddWithdrawalAsync(withdrawal, cancellationToken);

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
        CreatedAt = goal.CreatedAt
    };
}
