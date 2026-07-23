using FinanceApp.Application.DTOs.Debt;
using FinanceApp.Application.DTOs.SavingsGoal;
using FinanceApp.Application.Interfaces;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.Interfaces.Repositories;

namespace FinanceApp.Application.Services;

public class DebtService : IDebtService
{
    private readonly IDebtRepository _debtRepository;
    private readonly ISavingsGoalService _savingsGoalService;

    public DebtService(IDebtRepository debtRepository, ISavingsGoalService savingsGoalService)
    {
        _debtRepository = debtRepository;
        _savingsGoalService = savingsGoalService;
    }

    public async Task<IEnumerable<DebtResponseDto>> GetAllAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        var debts = await _debtRepository.GetByUserIdAsync(userId, cancellationToken);
        return debts.Select(MapToResponseDto);
    }

    public async Task<DebtResponseDto> GetByIdAsync(
        Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var debt = await _debtRepository.GetByIdAsync(id, cancellationToken);

        if (debt == null || debt.UserId != userId || debt.IsDeleted)
            throw new NotFoundException("Deuda", id);

        return MapToResponseDto(debt);
    }

    public async Task<DebtResponseDto> CreateAsync(
        Guid userId, DebtCreateDto dto, CancellationToken cancellationToken = default)
    {
        var type = Enum.Parse<DebtType>(dto.Type.Replace("_", ""), ignoreCase: true);

        if (dto.DueDay is < 1 or > 31)

            throw new DomainException(
                "INVALID_DUE_DAY",
                "El día de vencimiento debe estar entre 1 y 31.");

        var debt = new Debt
        {
            UserId = userId,
            Name = dto.Name.Trim(),
            Type = type,
            Creditor = dto.Creditor?.Trim(),
            OriginalAmount = dto.OriginalAmount,
            CurrentBalance = dto.CurrentBalance,
            InterestRate = dto.InterestRate,
            MinimumPayment = dto.MinimumPayment,
            DueDay = dto.DueDay,
            StartDate = dto.StartDate,
            TargetPayoffDate = dto.TargetPayoffDate,
            Notes = dto.Notes?.Trim(),
            IsActive = true
        };

        await _debtRepository.CreateAsync(debt, cancellationToken);
        return MapToResponseDto(debt);
    }

    public async Task<DebtResponseDto> UpdateAsync(
        Guid id, Guid userId, DebtUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var debt = await _debtRepository.GetByIdAsync(id, cancellationToken);

        if (debt == null || debt.UserId != userId || debt.IsDeleted)
            throw new NotFoundException("Deuda", id);

        if (dto.DueDay is < 1 or > 31)
            throw new DomainException(
                "INVALID_DUE_DAY",
                "El día de vencimiento debe estar entre 1 y 31.");

        debt.Name = dto.Name.Trim();
        debt.Creditor = dto.Creditor?.Trim();
        debt.CurrentBalance = dto.CurrentBalance;
        debt.InterestRate = dto.InterestRate;
        debt.MinimumPayment = dto.MinimumPayment;
        debt.DueDay = dto.DueDay;
        debt.TargetPayoffDate = dto.TargetPayoffDate;
        debt.IsActive = dto.IsActive;
        debt.Notes = dto.Notes?.Trim();

        await _debtRepository.UpdateAsync(debt, cancellationToken);
        return MapToResponseDto(debt);
    }

    public async Task DeleteAsync(
        Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var debt = await _debtRepository.GetByIdAsync(id, cancellationToken);

        if (debt == null || debt.UserId != userId || debt.IsDeleted)
            throw new NotFoundException("Deuda", id);

        debt.DeletedAt = DateTimeOffset.UtcNow;
        await _debtRepository.UpdateAsync(debt, cancellationToken);
    }

    public async Task<DebtSummaryDto> GetSummaryAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        var debts = (await _debtRepository.GetByUserIdAsync(userId, cancellationToken))
            .Where(d => d.IsActive)
            .ToList();

        var totalOriginal = debts.Sum(d => d.OriginalAmount);
        var totalCurrentBalance = debts.Sum(d => d.CurrentBalance);
        var totalPaid = totalOriginal - totalCurrentBalance;

        var byType = debts
            .GroupBy(d => d.Type)
            .Select(g => new DebtByTypeDto
            {
                Type = g.Key.ToString().ToLower(),
                CurrentBalance = g.Sum(d => d.CurrentBalance),
                Percentage = totalCurrentBalance > 0
                    ? Math.Round(g.Sum(d => d.CurrentBalance) / totalCurrentBalance * 100, 2)
                    : 0
            })
            .OrderByDescending(x => x.CurrentBalance)
            .ToList();

        var upcomingPayments = debts
            .Where(d => d.DueDay.HasValue && !d.IsPaidOff)
            .OrderBy(d => d.DueDay)
            .Select(d => new UpcomingPaymentDto
            {
                DebtId = d.Id,
                DebtName = d.Name,
                DueDay = d.DueDay!.Value,
                MinimumPayment = d.MinimumPayment
            })
            .ToList();

        return new DebtSummaryDto
        {
            TotalOriginal = totalOriginal,
            TotalCurrentBalance = totalCurrentBalance,
            TotalPaid = totalPaid,
            TotalPaidPercentage = totalOriginal > 0
                ? Math.Round(totalPaid / totalOriginal * 100, 2)
                : 0,
            ByType = byType,
            UpcomingPayments = upcomingPayments
        };
    }

    public async Task<IEnumerable<DebtPaymentResponseDto>> GetPaymentsAsync(
        Guid debtId, Guid userId, CancellationToken cancellationToken = default)
    {
        var debt = await _debtRepository.GetByIdAsync(debtId, cancellationToken);

        if (debt == null || debt.UserId != userId || debt.IsDeleted)
            throw new NotFoundException("Deuda", debtId);

        return debt.Payments
            .OrderByDescending(p => p.PaymentDate)
            .Select(MapPaymentToResponseDto);
    }

    public async Task<DebtPaymentResponseDto> AddPaymentAsync(
        Guid debtId, Guid userId, DebtPaymentCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        var debt = await _debtRepository.GetByIdAsync(debtId, cancellationToken);
        if (debt == null || debt.UserId != userId || debt.IsDeleted)
            throw new NotFoundException("Deuda", debtId);
        if (debt.IsPaidOff)
            throw new DomainException("IS_PAIDOFF", "Esta deuda ya fue liquidada.");
        if (dto.PrincipalAmount <= 0)
            throw new DomainException("INVALID_PRINCIPAL_AMOUNT", "El monto a capital debe ser mayor a 0.");

        if (debt.LinkedSavingsGoalId.HasValue)
        {
            await _savingsGoalService.DepositAsync(
                debt.LinkedSavingsGoalId.Value, userId,
                new DepositDto { Amount = dto.PrincipalAmount, Notes = $"Auto: abono deuda '{debt.Name}'" },
                cancellationToken);
        }

        var payment = new DebtPayment
        {
            DebtId = debtId,
            PaymentDate = dto.PaymentDate,
            Amount = dto.Amount,
            PrincipalAmount = dto.PrincipalAmount,
            InterestAmount = dto.InterestAmount,
            Notes = dto.Notes?.Trim()
        };

        debt.Payments.Add(payment);
        debt.CurrentBalance = Math.Max(0, debt.CurrentBalance - dto.PrincipalAmount);

        await _debtRepository.UpdateAsync(debt, cancellationToken);
        return MapPaymentToResponseDto(payment);
    }

    public async Task<DebtWithdrawalResponseDto> AddWithdrawalAsync(
        Guid debtId, Guid userId, DebtWithdrawalCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        var debt = await _debtRepository.GetByIdAsync(debtId, cancellationToken);
        if (debt == null || debt.UserId != userId || debt.IsDeleted)
            throw new NotFoundException("Deuda", debtId);
        if (dto.Amount <= 0)
            throw new DomainException("INVALID_WITHDRAWAL_AMOUNT", "El monto debe ser mayor a 0.");

        if (debt.LinkedSavingsGoalId.HasValue)
        {
            await _savingsGoalService.WithdrawAsync(
                debt.LinkedSavingsGoalId.Value, userId,
                new SavingsGoalWithdrawalCreateDto
                {
                    Amount = dto.Amount,
                    WithdrawalDate = dto.WithdrawalDate,
                    Reason = SavingsWithdrawalReason.ReallocatedToLiquid,
                    Notes = $"Auto: préstamo contra fondo, deuda '{debt.Name}'"
                },
                cancellationToken);
        }

        debt.OriginalAmount += dto.Amount;
        debt.CurrentBalance += dto.Amount;

        var withdrawal = new DebtWithdrawal
        {
            DebtId = debtId,
            WithdrawalDate = dto.WithdrawalDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            Amount = dto.Amount,
            Notes = dto.Notes?.Trim()
        };
        debt.Withdrawals.Add(withdrawal);

        await _debtRepository.UpdateAsync(debt, cancellationToken);

        return new DebtWithdrawalResponseDto
        {
            Id = withdrawal.Id,
            WithdrawalDate = withdrawal.WithdrawalDate,
            Amount = withdrawal.Amount,
            Notes = withdrawal.Notes,
            CreatedAt = withdrawal.CreatedAt,
            DebtCurrentBalanceAfter = debt.CurrentBalance
        };
    }

    private static DebtResponseDto MapToResponseDto(Debt debt) => new()
    {
        Id = debt.Id,
        Name = debt.Name,
        Type = debt.Type.ToString().ToLower(),
        Creditor = debt.Creditor,
        OriginalAmount = debt.OriginalAmount,
        CurrentBalance = debt.CurrentBalance,
        AmountPaid = debt.AmountPaid,
        PaidPercentage = debt.PaidPercentage,
        InterestRate = debt.InterestRate,
        MinimumPayment = debt.MinimumPayment,
        DueDay = debt.DueDay,
        StartDate = debt.StartDate,
        TargetPayoffDate = debt.TargetPayoffDate,
        IsActive = debt.IsActive,
        IsPaidOff = debt.IsPaidOff,
        Notes = debt.Notes,
        CreatedAt = debt.CreatedAt
    };

    private static DebtPaymentResponseDto MapPaymentToResponseDto(DebtPayment payment) => new()
    {
        Id = payment.Id,
        PaymentDate = payment.PaymentDate,
        Amount = payment.Amount,
        PrincipalAmount = payment.PrincipalAmount,
        InterestAmount = payment.InterestAmount,
        Notes = payment.Notes,
        CreatedAt = payment.CreatedAt
    };
}