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
    private readonly IBusinessDateProvider _businessDateProvider;
    private readonly IUnitOfWork _unitOfWork;

    public FinancialAccountService(
        IFinancialAccountRepository accountRepository,
        ISavingsGoalRepository savingsGoalRepository,
        IInvestmentRepository investmentRepository,
        IUnitOfWork unitOfWork,
        IBusinessDateProvider? businessDateProvider = null)
    {
        _accountRepository = accountRepository;
        _savingsGoalRepository = savingsGoalRepository;
        _unitOfWork = unitOfWork;
        _businessDateProvider = businessDateProvider ?? new EcuadorBusinessDateProvider(TimeProvider.System);
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
            TransferId = t.TransferId,
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

        var openingDate = dto.OpeningDate ?? _businessDateProvider.Today;
        if (openingDate > _businessDateProvider.Today)
            throw new DomainException(
                "INVALID_OPENING_DATE",
                "La fecha de apertura no puede ser futura.");

        var account = new FinancialAccount
        {
            UserId = userId,
            Name = dto.Name.Trim(),
            Type = type,
            CurrentBalance = dto.OpeningBalance,
            OpeningBalance = dto.OpeningBalance,
            OpeningDate = openingDate,
            IsDefault = shouldBeDefault,
            IsSystem = false,
            IsActive = true
        };
        await _accountRepository.CreateAsync(account, cancellationToken);

        await _accountRepository.SaveTransactionAsync(new AccountTransaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AccountId = account.Id,
            Amount = dto.OpeningBalance,
            Date = openingDate,
            Description = "Apertura de cuenta (no es un ingreso)",
            SourceType = "account-opening",
            SourceId = account.Id
        }, cancellationToken);

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
                Id = Guid.NewGuid(),
                UserId = userId,
                AccountId = account.Id,
                Amount = balanceDifference,
                Date = _businessDateProvider.Today,
                Description = "Ajuste de saldo",
                SourceType = "account-adjustment",
                SourceId = Guid.NewGuid()
            }, cancellationToken);
        }

        return Map(account);
    }

    public async Task<AccountTransferResponseDto> TransferAsync(
        Guid userId, AccountTransferCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto.IdempotencyKey == Guid.Empty)
            throw new DomainException("INVALID_IDEMPOTENCY_KEY", "La clave de idempotencia es obligatoria.");
        if (dto.FromAccountId == Guid.Empty || dto.ToAccountId == Guid.Empty)
            throw new DomainException("INVALID_ACCOUNT", "Debes seleccionar las cuentas de origen y destino.");
        if (dto.FromAccountId == dto.ToAccountId)
            throw new DomainException("SAME_TRANSFER_ACCOUNT", "La cuenta de origen debe ser distinta de la cuenta de destino.");
        if (dto.Amount <= 0)
            throw new DomainException("INVALID_TRANSFER_AMOUNT", "El monto de la transferencia debe ser mayor que cero.");
        if (dto.Date == default)
            throw new DomainException("INVALID_TRANSFER_DATE", "La fecha de la transferencia es obligatoria.");
        if (dto.Description?.Length > 300)
            throw new DomainException("INVALID_TRANSFER_DESCRIPTION", "La descripción no puede superar 300 caracteres.");

        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var existing = await _accountRepository.GetTransactionsByTransferIdAsync(
                userId, dto.IdempotencyKey, ct);
            if (existing.Count != 0)
                return MapExistingTransfer(existing, dto.IdempotencyKey);

            var from = await GetActiveOwnedAccountAsync(dto.FromAccountId, userId, ct);
            var to = await GetActiveOwnedAccountAsync(dto.ToAccountId, userId, ct);
            if (from.CurrentBalance < dto.Amount)
                throw new DomainException(
                    "INSUFFICIENT_ACCOUNT_BALANCE",
                    "La cuenta de origen no tiene saldo suficiente para esta transferencia.");

            var description = string.IsNullOrWhiteSpace(dto.Description)
                ? $"Transferencia a {to.Name}"
                : dto.Description.Trim();

            from.CurrentBalance -= dto.Amount;
            to.CurrentBalance += dto.Amount;
            await _accountRepository.UpdateAsync(from, ct);
            await _accountRepository.UpdateAsync(to, ct);

            await _accountRepository.SaveTransactionAsync(new AccountTransaction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AccountId = from.Id,
                Amount = -dto.Amount,
                Date = dto.Date,
                Description = description,
                SourceType = "account-transfer:out",
                SourceId = dto.IdempotencyKey,
                TransferId = dto.IdempotencyKey
            }, ct);
            await _accountRepository.SaveTransactionAsync(new AccountTransaction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AccountId = to.Id,
                Amount = dto.Amount,
                Date = dto.Date,
                Description = description,
                SourceType = "account-transfer:in",
                SourceId = dto.IdempotencyKey,
                TransferId = dto.IdempotencyKey
            }, ct);

            return new AccountTransferResponseDto
            {
                TransferId = dto.IdempotencyKey,
                FromAccountId = from.Id,
                FromAccountName = from.Name,
                ToAccountId = to.Id,
                ToAccountName = to.Name,
                Amount = dto.Amount,
                Date = dto.Date,
                Description = description
            };
        }, cancellationToken);
    }
    public async Task<FinancialAccountResponseDto> GetOrCreateDefaultAsync(
        Guid userId, FinancialAccountType type,
        CancellationToken cancellationToken = default) =>
        Map(await GetOrCreateDefaultEntityAsync(userId, type, cancellationToken));

    public async Task<decimal> GetAvailableBalanceAsync(
        Guid userId, Guid? accountId, FinancialAccountType fallbackType,
        CancellationToken cancellationToken = default)
    {
        var account = accountId.HasValue
            ? await _accountRepository.GetByIdAsync(accountId.Value, cancellationToken)
            : await GetOrCreateDefaultEntityAsync(userId, fallbackType, cancellationToken);

        if (account == null || account.UserId != userId || account.Type != fallbackType
            || !account.IsActive || account.IsDeleted)
            throw new DomainException("INVALID_ACCOUNT", "La cuenta programada no está disponible.");

        return account.CurrentBalance;
    }

    public async Task SyncMovementAsync(
        Guid userId, Guid? accountId, FinancialAccountType fallbackType,
        decimal signedAmount, DateOnly date, string sourceType, Guid sourceId,
        string description, CancellationToken cancellationToken = default)
    {
        var existingTransaction = await _accountRepository.GetTransactionBySourceAsync(
            userId, sourceType, sourceId, cancellationToken);

        // Eliminar revierte el movimiento histórico aun si su cuenta ya fue inactivada.
        if (signedAmount == 0)
        {
            if (existingTransaction == null)
                return;

            var previousAccount = await _accountRepository.GetByIdAsync(
                existingTransaction.AccountId, cancellationToken);
            if (previousAccount != null && previousAccount.UserId == userId)
            {
                previousAccount.CurrentBalance -= existingTransaction.Amount;
                await _accountRepository.UpdateAsync(previousAccount, cancellationToken);
            }

            await _accountRepository.DeleteTransactionAsync(existingTransaction, cancellationToken);
            return;
        }

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
                Id = Guid.NewGuid(),
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

    public async Task SyncTransferBetweenAccountsAsync(
        Guid userId, Guid? fromAccountId, FinancialAccountType fromFallbackType,
        Guid? toAccountId, FinancialAccountType toFallbackType,
        decimal amount, DateOnly date, string sourceType, Guid sourceId,
        string description, CancellationToken cancellationToken = default)
    {
        await SyncMovementAsync(
            userId, fromAccountId, fromFallbackType, -amount, date,
            $"{sourceType}:out", sourceId, description, cancellationToken);
        await SyncMovementAsync(
            userId, toAccountId, toFallbackType, amount, date,
            $"{sourceType}:in", sourceId, description, cancellationToken);
    }

    private async Task<FinancialAccount> GetOrCreateDefaultEntityAsync(
        Guid userId, FinancialAccountType type, CancellationToken cancellationToken)
    {
        var existing = await _accountRepository.GetDefaultAsync(userId, type, cancellationToken);
        if (existing != null)
            return existing;

        var initialBalance = type switch
        {
            FinancialAccountType.Savings => 0,
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

    private async Task<FinancialAccount> GetActiveOwnedAccountAsync(
        Guid accountId, Guid userId, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByIdAsync(accountId, cancellationToken);
        if (account == null || account.UserId != userId || account.IsDeleted)
            throw new NotFoundException("Cuenta", accountId);
        if (!account.IsActive)
            throw new DomainException("INACTIVE_ACCOUNT", "La cuenta seleccionada está inactiva.");
        return account;
    }

    private static AccountTransferResponseDto MapExistingTransfer(
        IReadOnlyList<AccountTransaction> transactions, Guid transferId)
    {
        var outgoing = transactions.SingleOrDefault(t => t.SourceType == "account-transfer:out");
        var incoming = transactions.SingleOrDefault(t => t.SourceType == "account-transfer:in");
        if (transactions.Count != 2 || outgoing == null || incoming == null
            || outgoing.Amount >= 0 || incoming.Amount <= 0
            || -outgoing.Amount != incoming.Amount)
            throw new DomainException(
                "INCOMPLETE_TRANSFER",
                "La transferencia existente está incompleta y requiere revisión.");

        return new AccountTransferResponseDto
        {
            TransferId = transferId,
            FromAccountId = outgoing.AccountId,
            FromAccountName = outgoing.Account.Name,
            ToAccountId = incoming.AccountId,
            ToAccountName = incoming.Account.Name,
            Amount = incoming.Amount,
            Date = outgoing.Date,
            Description = outgoing.Description
        };
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
        OpeningBalance = account.OpeningBalance,
        OpeningDate = account.OpeningDate,
        IsDefault = account.IsDefault,
        IsSystem = account.IsSystem,
        IsActive = account.IsActive
    };
}