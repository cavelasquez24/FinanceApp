using FinanceApp.Application.DTOs.SavingsGoal;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Exceptions;

namespace FinanceApp.Application.Services;

public partial class EmergencyFundRestorationService
{
    public async Task<DueRestorationProcessingResultDto> ProcessDueAsync(
        Guid userId, DateOnly asOfDate, CancellationToken cancellationToken = default)
    {
        var utcToday = DateOnly.FromDateTime(DateTime.UtcNow);
        if (asOfDate > utcToday)
            throw new DomainException(
                "INVALID_PROCESSING_DATE",
                "No se pueden ejecutar aportes con una fecha futura.");

        var result = new DueRestorationProcessingResultDto();
        var dueIds = await _restorationRepository.GetDueIdsAsync(
            userId, asOfDate, cancellationToken);

        foreach (var id in dueIds)
        {
            while (true)
            {
                var restoration = await _restorationRepository.GetOwnedByIdAsync(
                    id, userId, cancellationToken);
                if (restoration == null
                    || restoration.Status != EmergencyFundRestorationStatus.Open
                    || restoration.NextScheduledDate > asOfDate)
                    break;

                var amount = Math.Min(
                    restoration.ScheduledContributionAmount,
                    restoration.OutstandingAmount);
                var available = await _accountService.GetAvailableBalanceAsync(
                    userId, restoration.ScheduledSourceAccountId,
                    FinancialAccountType.Cash, cancellationToken);

                if (available < amount)
                {
                    result.InsufficientFundsCount++;
                    break;
                }

                var scheduledDate = restoration.NextScheduledDate;
                await RegisterPaymentAsync(id, userId, new EmergencyFundRestorationPaymentDto
                {
                    Amount = amount,
                    PaymentDate = scheduledDate,
                    SourceAccountId = restoration.ScheduledSourceAccountId,
                    Notes = $"Aporte programado: {restoration.Description}"
                }, cancellationToken);

                result.ProcessedCount++;
                result.ProcessedAmount += amount;
            }
        }

        return result;
    }
}
