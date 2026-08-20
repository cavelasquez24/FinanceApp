using FinanceApp.Application.DTOs.Account;
using FinanceApp.Application.Interfaces;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.Interfaces.Repositories;
using FinanceApp.Domain.Interfaces.Services;

namespace FinanceApp.Application.Services;

public class AccountReconciliationService : IAccountReconciliationService
{
    private readonly IAccountReconciliationRepository _reconciliationRepository;
    private readonly IFinancialAccountRepository _accountRepository;
    private readonly IBusinessDateProvider _businessDateProvider;

    public AccountReconciliationService(
        IAccountReconciliationRepository reconciliationRepository,
        IFinancialAccountRepository accountRepository,
        IBusinessDateProvider businessDateProvider)
    {
        _reconciliationRepository = reconciliationRepository;
        _accountRepository = accountRepository;
        _businessDateProvider = businessDateProvider;
    }

    public async Task<ReconciliationPreviewDto> GetPreviewAsync(
        Guid accountId, Guid userId,
        CancellationToken cancellationToken = default)
    {
        var account = await GetAccountOrThrowAsync(accountId, userId, cancellationToken);

        var ledgerBalance = await _reconciliationRepository.GetLedgerBalanceAsync(
            accountId, cancellationToken);

        var last = await _reconciliationRepository.GetLastByAccountAsync(
            accountId, userId, cancellationToken);

        return new ReconciliationPreviewDto(
            AccountId: account.Id,
            AccountName: account.Name,
            LedgerBalance: ledgerBalance,
            CurrentBalance: account.CurrentBalance,
            LastReconciliationDate: last?.ReconciliationDate,
            LastReconciliationActualBalance: last?.ActualBalance
        );
    }

    public async Task<ReconciliationResponseDto> ApplyAsync(
        Guid accountId, Guid userId, ReconciliationCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto.ReconciliationDate == default)
            throw new DomainException("INVALID_DATE", "La fecha de conciliación es obligatoria.");

        var account = await GetAccountOrThrowAsync(accountId, userId, cancellationToken);

        var expectedBalance = await _reconciliationRepository.GetLedgerBalanceAsync(
            accountId, cancellationToken);

        var difference = dto.ActualBalance - expectedBalance;

        AccountTransaction? adjustment = null;

        if (difference != 0)
        {
            adjustment = new AccountTransaction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AccountId = accountId,
                Amount = difference,
                Date = dto.ReconciliationDate,
                Description = $"Ajuste de conciliación ({(difference > 0 ? "+" : "")}{difference:F2})",
                SourceType = "account-adjustment",
                SourceId = Guid.NewGuid()
            };

            await _accountRepository.SaveTransactionAsync(adjustment, cancellationToken);
            account.CurrentBalance = dto.ActualBalance;
            await _accountRepository.UpdateAsync(account, cancellationToken);
        }

        var reconciliation = new AccountReconciliation
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AccountId = accountId,
            ReconciliationDate = dto.ReconciliationDate,
            ExpectedBalance = expectedBalance,
            ActualBalance = dto.ActualBalance,
            Difference = difference,
            AdjustmentTransactionId = adjustment?.Id,
            Notes = dto.Notes?.Trim(),
            Status = ReconciliationStatus.Reconciled
        };

        await _reconciliationRepository.CreateAsync(reconciliation, cancellationToken);

        return MapToDto(reconciliation, account.Name);
    }

    public async Task<IReadOnlyList<ReconciliationResponseDto>> GetHistoryAsync(
        Guid accountId, Guid userId, int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var account = await GetAccountOrThrowAsync(accountId, userId, cancellationToken);

        var items = await _reconciliationRepository.GetByAccountAsync(
            accountId, userId, page, pageSize, cancellationToken);

        return items.Select(r => MapToDto(r, account.Name)).ToList();
    }

    private async Task<FinancialAccount> GetAccountOrThrowAsync(
        Guid accountId, Guid userId, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByIdAsync(accountId, cancellationToken);
        if (account == null || account.UserId != userId || account.IsDeleted)
            throw new NotFoundException("Cuenta", accountId);
        if (!account.IsActive)
            throw new DomainException("ACCOUNT_INACTIVE", "No se puede conciliar una cuenta inactiva.");
        return account;
    }

    private static ReconciliationResponseDto MapToDto(AccountReconciliation r, string accountName) =>
        new(
            Id: r.Id,
            AccountId: r.AccountId,
            AccountName: accountName,
            ReconciliationDate: r.ReconciliationDate,
            ExpectedBalance: r.ExpectedBalance,
            ActualBalance: r.ActualBalance,
            Difference: r.Difference,
            AdjustmentCreated: r.AdjustmentTransactionId.HasValue,
            AdjustmentTransactionId: r.AdjustmentTransactionId,
            Notes: r.Notes,
            Status: r.Status.ToString(),
            CreatedAt: r.CreatedAt
        );
}
