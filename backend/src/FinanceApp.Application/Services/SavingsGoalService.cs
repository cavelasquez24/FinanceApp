using FinanceApp.Application.DTOs.SavingsGoal;
using FinanceApp.Application.Interfaces;
using FinanceApp.Domain.Entities;
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
        return MapToResponseDto(goal);
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
        // Estimamos meses restantes basado en el promedio de aportes
        // Por ahora retornamos null — en v2 calculamos con historial
        EstimatedMonthsToComplete = null,
        CreatedAt = goal.CreatedAt
    };
}