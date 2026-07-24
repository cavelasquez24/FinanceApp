using FinanceApp.Application.DTOs.Account;
using FinanceApp.Application.Interfaces;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.Interfaces.Repositories;

namespace FinanceApp.Application.Services;

public class FinancialAccountService : IFinancialAccountService
{
    private readonly IFinancialAccountRepository _accountRepository;
    private readonly ISavingsGoalRepository _savingsGoalRepository;
    private readonly IInvestmentRepository _investmentRepository;

    public FinancialAccountService(
        IFinancialAccountRepository accountRepository,
        ISavingsGoalRepository savingsGoalRepository,
        IInvestmentRepository investmentRepository)
    {
        _accountRepository = accountRepository;
        _savingsGoalRepository = savingsGoalRepository;
        _investmentRepository = investmentRepository;
    }

    public async Task<IReadOnlyList<FinancialAccountResponseDto>> GetAllAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        await GetOrCreateDefaultEntityAsync(userId, FinancialAccountType.Cash, cancellationToken);
        await GetOrCreateDefaultEntityAsync(userId, FinancialAccountType.Savings, cancellationToken);
        await GetOrCreateDefaultEntityAsync(userId, FinancialAccountType.Investment, cancellationToken);

        var accounts = await _accountRepository.GetByUserIdAsync(userId, cancellationToken);
        return accounts.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<AccountTransactionResponseDto>> GetRecentTransactionsAsync(
        Guid userId, int count, CancellationToken cancellationToken = default)
    {
        var items = await _accountRepository.GetRecentTransactionsAsync(
            userId, Math.Clamp(count, 1, 100), cancellationToken);
        return items.Select(t => new AccountTransactionResponseDto
        {
            Id = t.Id,
            AccountId = t.AccountId,
            AccountName = t.Account.Name,
            Amount = t.Amount,
            Date = t.Date,
            Description = t.Description
        }).ToList();
    }

    public async Task<FinancialAccountResponseDto> CreateAsync(
        Guid userId, FinancialAccountCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new DomainException("INVALID_ACCOUNT_NAME", "El nombre de la cuenta es obligatorio.");

        if (!Enum.TryParse<FinancialAccountType>(dto.Type, true, out var type))
            throw new DomainException("INVALID_ACCOUNT_TYPE", "El tipo de cuenta no es válido.");

        var existing = await _accountRepository.GetByUserIdAsync(userId, cancellationToken);
        var shouldBeDefault = dto.IsDefault || existing.All(a => a.Type != type || !a.IsActive);
        if (shouldBeDefault)
            await ClearDefaultAsync(existing.Where(a => a.Type == type), cancellationToken);

        var account = new FinancialAccount
        {
            UserId = userId,
            Name = dto.Name.Trim(),
            Type = type,
            CurrentBalance = dto.OpeningBalance,
            IsDefault = shouldBeDefault,
            IsSystem = false,
            IsActive = true
        };
        await _accountRepository.CreateAsync(account, cancellationToken);

        if (dto.OpeningBalance != 0)
        {
            await _accountRepository.SaveTransactionAsync(new AccountTransaction
            {
                UserId = userId,
                AccountId = account.Id,
                Amount = dto.OpeningBalance,
                Date = DateOnly.FromDateTime(DateTime.Today),
                Description = "Saldo inicial",
                SourceType = "account-opening",
                SourceId = account.Id
            }, cancellationToken);
        }

        return Map(account);
    }

    public async Task<FinancialAccountResponseDto> UpdateAsync(
        Guid id, Guid userId, FinancialAccountUpdateDto dto,
        CancellationToken cancellationToken = default)
    {
        var account = await _accountRepository.GetByIdAsync(id, cancellationToken);
        if (account == null || account.UserId != userId || account.IsDeleted)
            throw new NotFoundException("Cuenta", id);
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new DomainException("INVALID_ACCOUNT_NAME", "El nombre de la cuenta es obligatorio.");

        if (dto.IsDefault)
        {
            var accounts = await _accountRepository.GetByUserIdAsync(userId, cancellationToken);
            await ClearDefaultAsync(
                accounts.Where(a => a.Type == account.Type && a.Id != account.Id),
                cancellationToken);
        }

        var balanceDifference = dto.CurrentBalance - account.CurrentBalance;
        account.Name = dto.Name.Trim();
        account.CurrentBalance = dto.CurrentBalance;
        account.IsDefault = dto.IsDefault;
        account.IsActive = dto.IsActive;
        await _accountRepository.UpdateAsync(account, cancellationToken);

        if (balanceDifference != 0)
        {
            await _accountRepository.SaveTransactionAsync(new AccountTransaction
            {
                UserId = userId,
                AccountId = account.Id,
                Amount = balanceDifference,
                Date = DateOnly.FromDateTime(DateTime.Today),
                Description = "Ajuste de saldo",
                SourceType = "account-adjustment",
                SourceId = Guid.NewGuid()
            }, cancellationToken);
        }

        return Map(account);
    }

    public async Task<FinancialAccountResponseDto> GetOrCreateDefaultAsync(
        Guid userId, FinancialAccountType type,
        CancellationToken cancellationToken = default) =>
        Map(await GetOrCreateDefaultEntityAsync(userId, type, cancellationToken));

    public async Task SyncMovementAsync(
        Guid userId, Guid? accountId, FinancialAccountType fallbackType,
        decimal signedAmount, DateOnly date, string sourceType, Guid sourceId,
        string description, CancellationToken cancellationToken = default)
    {
        var account = accountId.HasValue
            ? await _accountRepository.GetByIdAsync(accountId.Value, cancellationToken)
            : await GetOrCreateDefaultEntityAsync(userId, fallbackType, cancellationToken);

        if (account == null || account.UserId != userId || !account.IsActive || account.IsDeleted)
            throw new DomainException("INVALID_ACCOUNT", "La cuenta seleccionada no está disponible.");

        var existing = await _accountRepository.GetTransactionBySourceAsync(
            userId, sourceType, sourceId, cancellationToken);

        if (existing != null && existing.AccountId != account.Id)
        {
            var previousAccount = await _accountRepository.GetByIdAsync(
                existing.AccountId, cancellationToken);
            if (previousAccount != null)
            {
                previousAccount.CurrentBalance -= existing.Amount;
                await _accountRepository.UpdateAsync(previousAccount, cancellationToken);
            }
            account.CurrentBalance += signedAmount;
        }
        else
        {
            account.CurrentBalance += signedAmount - (existing?.Amount ?? 0);
        }

        await _accountRepository.UpdateAsync(account, cancellationToken);

        if (signedAmount == 0)
        {
            if (existing != null)
                await _accountRepository.DeleteTransactionAsync(existing, cancellationToken);
            return;
        }

        if (existing == null)
        {
            existing = new AccountTransaction
            {
                UserId = userId,
                SourceType = sourceType,
                SourceId = sourceId
            };
        }

        existing.AccountId = account.Id;
        existing.Amount = signedAmount;
        existing.Date = date;
        existing.Description = description;
        await _accountRepository.SaveTransactionAsync(existing, cancellationToken);
    }

    public async Task SyncTransferAsync(
        Guid userId, FinancialAccountType fromType, FinancialAccountType toType,
        decimal amount, DateOnly date, string sourceType, Guid sourceId,
        string description, CancellationToken cancellationToken = default)
    {
        var from = await GetOrCreateDefaultEntityAsync(userId, fromType, cancellationToken);
        var to = await GetOrCreateDefaultEntityAsync(userId, toType, cancellationToken);
        await SyncMovementAsync(
            userId, from.Id, fromType, -amount, date, $"{sourceType}:out",
            sourceId, description, cancellationToken);
        await SyncMovementAsync(
            userId, to.Id, toType, amount, date, $"{sourceType}:in",
            sourceId, description, cancellationToken);
    }

    private async Task<FinancialAccount> GetOrCreateDefaultEntityAsync(
        Guid userId, FinancialAccountType type, CancellationToken cancellationToken)
    {
        var existing = await _accountRepository.GetDefaultAsync(userId, type, cancellationToken);
        if (existing != null)
            return existing;

        var initialBalance = type switch
        {
            FinancialAccountType.Savings =>
                await _savingsGoalRepository.GetTotalSavedAsync(userId, cancellationToken),
            FinancialAccountType.Investment =>
                await _investmentRepository.GetTotalCurrentValueAsync(userId, cancellationToken),
            _ => 0
        };

        var account = new FinancialAccount
        {
            UserId = userId,
            Name = type switch
            {
                FinancialAccountType.Cash => "Cuenta principal",
                FinancialAccountType.Savings => "Fondo de ahorro",
                _ => "Portafolio de inversión"
            },
            Type = type,
            CurrentBalance = initialBalance,
            IsDefault = true,
            IsSystem = true,
            IsActive = true
        };
        await _accountRepository.CreateAsync(account, cancellationToken);
        return account;
    }

    private async Task ClearDefaultAsync(
        IEnumerable<FinancialAccount> accounts, CancellationToken cancellationToken)
    {
        foreach (var account in accounts.Where(a => a.IsDefault))
        {
            account.IsDefault = false;
            await _accountRepository.UpdateAsync(account, cancellationToken);
        }
    }

    private static FinancialAccountResponseDto Map(FinancialAccount account) => new()
    {
        Id = account.Id,
        Name = account.Name,
        Type = account.Type.ToString().ToLowerInvariant(),
        CurrentBalance = account.CurrentBalance,
        IsDefault = account.IsDefault,
        IsSystem = account.IsSystem,
        IsActive = account.IsActive
    };
}
