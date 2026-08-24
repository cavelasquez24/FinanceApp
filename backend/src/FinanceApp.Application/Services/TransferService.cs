using FinanceApp.Application.DTOs.Transfer;
using FinanceApp.Application.Interfaces;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.Interfaces.Repositories;

namespace FinanceApp.Application.Services;

public class TransferService : ITransferService
{
    private readonly IAccountTransferRepository _transferRepository;
    private readonly IFinancialAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBusinessDateProvider _businessDateProvider;

    public TransferService(
        IAccountTransferRepository transferRepository,
        IFinancialAccountRepository accountRepository,
        IUnitOfWork unitOfWork,
        IBusinessDateProvider businessDateProvider)
    {
        _transferRepository = transferRepository;
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
        _businessDateProvider = businessDateProvider;
    }

    public async Task<AccountTransferCreateResultDto> CreateAsync(
        Guid userId, AccountTransferCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto.FromAccountId == dto.ToAccountId)
            throw new DomainException(
                "SAME_TRANSFER_ACCOUNT",
                "La cuenta de origen debe ser distinta de la cuenta de destino.");

        if (dto.Amount <= 0)
            throw new DomainException(
                "INVALID_TRANSFER_AMOUNT",
                "El monto de la transferencia debe ser mayor que cero.");

        if (dto.Description?.Length > 300)
            throw new DomainException(
                "INVALID_TRANSFER_DESCRIPTION",
                "La descripción no puede superar 300 caracteres.");

        var transferGroupId = dto.TransferGroupId ?? Guid.NewGuid();

        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var existing = await _transferRepository.GetByTransferGroupIdAsync(
                userId, transferGroupId, ct);
            if (existing != null)
                return new AccountTransferCreateResultDto
                {
                    Transfer = Map(existing),
                    InsufficientFundsWarning = false
                };

            var from = await GetActiveOwnedAccountAsync(dto.FromAccountId, userId, ct);
            var to = await GetActiveOwnedAccountAsync(dto.ToAccountId, userId, ct);

            var insufficientFundsWarning = from.CurrentBalance < dto.Amount;

            var transferDate = dto.TransferDate == default
                ? _businessDateProvider.Today
                : dto.TransferDate;

            var description = string.IsNullOrWhiteSpace(dto.Description)
                ? $"Transferencia a {to.Name}"
                : dto.Description.Trim();

            var transfer = new AccountTransfer
            {
                Id = transferGroupId,
                UserId = userId,
                FromAccountId = from.Id,
                ToAccountId = to.Id,
                Amount = dto.Amount,
                TransferDate = transferDate,
                Description = description,
                Status = TransferStatus.Completed,
                TransferGroupId = transferGroupId
            };
            await _transferRepository.CreateAsync(transfer, ct);

            from.CurrentBalance -= dto.Amount;
            to.CurrentBalance += dto.Amount;
            await _accountRepository.UpdateAsync(from, ct);
            await _accountRepository.UpdateAsync(to, ct);

            // SourceType usa sufijo :out/:in porque AccountTransfer
            // genera dos AccountTransactions simultáneas — único
            // SourceType del ledger con esta característica.
            // Precedente: FinancialAccountService.SyncTransferBetweenAccountsAsync
            await _accountRepository.SaveTransactionAsync(new AccountTransaction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AccountId = from.Id,
                Amount = -dto.Amount,
                Date = transferDate,
                Description = description,
                SourceType = "account-transfer:out",
                SourceId = transfer.Id,
                TransferId = transfer.TransferGroupId
            }, ct);
            await _accountRepository.SaveTransactionAsync(new AccountTransaction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                AccountId = to.Id,
                Amount = dto.Amount,
                Date = transferDate,
                Description = description,
                SourceType = "account-transfer:in",
                SourceId = transfer.Id,
                TransferId = transfer.TransferGroupId
            }, ct);

            transfer.FromAccount = from;
            transfer.ToAccount = to;

            return new AccountTransferCreateResultDto
            {
                Transfer = Map(transfer),
                InsufficientFundsWarning = insufficientFundsWarning
            };
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<AccountTransferSummaryDto>> GetByUserIdAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        var transfers = await _transferRepository.GetByUserIdAsync(userId, cancellationToken);
        return transfers.Select(MapSummary).ToList();
    }

    public async Task<AccountTransferDto> GetByIdAsync(
        Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var transfer = await _transferRepository.GetOwnedByIdAsync(id, userId, cancellationToken);
        if (transfer == null)
            throw new NotFoundException("Transferencia", id);

        return Map(transfer);
    }

    public async Task CancelAsync(
        Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var transfer = await _transferRepository.GetOwnedByIdAsync(id, userId, cancellationToken);
        if (transfer == null)
            throw new NotFoundException("Transferencia", id);

        // En MVP Status siempre sale Completed — esta rama
        // es alcanzable cuando se implemente el flujo Pending
        // (transferencias programadas o en tránsito).
        if (transfer.Status != TransferStatus.Pending)
            throw new DomainException(
                "TRANSFER_NOT_CANCELLABLE",
                "Solo transferencias pendientes pueden cancelarse.");

        transfer.Status = TransferStatus.Cancelled;
        transfer.DeletedAt = DateTimeOffset.UtcNow;
        await _transferRepository.UpdateAsync(transfer, cancellationToken);
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

    private static AccountTransferDto Map(AccountTransfer transfer) => new()
    {
        Id = transfer.Id,
        FromAccountId = transfer.FromAccountId,
        FromAccountName = transfer.FromAccount.Name,
        ToAccountId = transfer.ToAccountId,
        ToAccountName = transfer.ToAccount.Name,
        Amount = transfer.Amount,
        TransferDate = transfer.TransferDate,
        Description = transfer.Description,
        Status = transfer.Status.ToString().ToLowerInvariant(),
        TransferGroupId = transfer.TransferGroupId,
        CreatedAt = transfer.CreatedAt
    };

    private static AccountTransferSummaryDto MapSummary(AccountTransfer transfer) => new()
    {
        Id = transfer.Id,
        FromAccountId = transfer.FromAccountId,
        FromAccountName = transfer.FromAccount.Name,
        ToAccountId = transfer.ToAccountId,
        ToAccountName = transfer.ToAccount.Name,
        Amount = transfer.Amount,
        TransferDate = transfer.TransferDate,
        Description = transfer.Description,
        Status = transfer.Status.ToString().ToLowerInvariant(),
        CreatedAt = transfer.CreatedAt
    };
}
