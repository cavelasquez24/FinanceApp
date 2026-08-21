using FinanceApp.Application.DTOs.Investment;
using FinanceApp.Application.Interfaces;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.Interfaces.Repositories;

namespace FinanceApp.Application.Services;

public class InvestmentService : IInvestmentService
{
    private readonly IInvestmentRepository _investmentRepository;
    private readonly IFinancialAccountService? _accountService;
    private readonly IUnitOfWork? _unitOfWork;

    public InvestmentService(
        IInvestmentRepository investmentRepository,
        IFinancialAccountService? accountService = null,
        IUnitOfWork? unitOfWork = null)
    {
        _investmentRepository = investmentRepository;
        _accountService = accountService;
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<InvestmentResponseDto>> GetAllAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        var investments = await _investmentRepository.GetByUserIdAsync(
            userId, cancellationToken);
        return investments.Select(MapToResponseDto);
    }

    public async Task<InvestmentResponseDto> GetByIdAsync(
        Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var investment = await _investmentRepository.GetByIdAsync(
            id, cancellationToken);
        if (investment == null || investment.UserId != userId || investment.IsDeleted)
            throw new NotFoundException("Inversión", id);
        return MapToResponseDto(investment);
    }

    public Task<InvestmentResponseDto> CreateAsync(
        Guid userId, InvestmentCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        return RunInTransactionAsync(async ct =>
        {
            // Determina el capital base: suma de aportes históricos si se proveen,
            // o dto.InitialAmount como fallback.
            var hasRealContributions = dto.IsHistoricalImport
                && !dto.IsConsolidatedSnapshot
                && dto.HistoricalContributions is { Count: > 0 };

            var contributedCapital = hasRealContributions
                ? dto.HistoricalContributions!.Sum(c => c.Amount)
                : dto.InitialAmount;

            var investment = new Investment
            {
                UserId = userId,
                Name = dto.Name.Trim(),
                Type = Enum.Parse<InvestmentType>(
                    dto.Type.Replace("_", ""), true),
                Ticker = dto.Ticker?.Trim().ToUpperInvariant(),
                Broker = dto.Broker?.Trim(),
                InitialAmount = contributedCapital,
                CurrentValue = dto.IsHistoricalImport
                    ? dto.CurrentValue ?? contributedCapital
                    : contributedCapital,
                PurchaseDate = dto.PurchaseDate,
                Notes = dto.IsHistoricalImport && dto.IsConsolidatedSnapshot
                    ? "Saldo de apertura consolidado — historial anterior no disponible"
                    : dto.Notes?.Trim(),
                IsActive = true
            };

            // Alta normal: crea la InvestmentContribution en cascada.
            if (!dto.IsHistoricalImport)
            {
                investment.Contributions.Add(new InvestmentContribution
                {
                    ContributionDate = dto.PurchaseDate,
                    Amount = dto.InitialAmount,
                    Notes = "Compra inicial"
                });
            }

            await _investmentRepository.CreateAsync(investment, ct);

            // InvestmentTransaction records según el modo de alta.
            if (!dto.IsHistoricalImport)
            {
                // CASO 1 — alta normal: un solo IT de Contribution.
                await _investmentRepository.AddTransactionAsync(new InvestmentTransaction
                {
                    InvestmentId = investment.Id,
                    TransactionType = InvestmentTransactionType.Contribution,
                    Amount = dto.InitialAmount,
                    TransactionDate = dto.PurchaseDate,
                    IsHistorical = false,
                    Notes = "Compra inicial"
                }, ct);
            }
            else if (!dto.IsConsolidatedSnapshot)
            {
                // CASO 2 — histórico con aportes reales: un IT por cada aporte.
                var contribs = hasRealContributions
                    ? dto.HistoricalContributions!
                    : [new HistoricalContributionDto
                        { ContributionDate = dto.PurchaseDate, Amount = dto.InitialAmount }];

                foreach (var hc in contribs)
                {
                    await _investmentRepository.AddTransactionAsync(new InvestmentTransaction
                    {
                        InvestmentId = investment.Id,
                        TransactionType = InvestmentTransactionType.HistoricalContribution,
                        Amount = hc.Amount,
                        TransactionDate = hc.ContributionDate,
                        IsHistorical = true,
                        Notes = hc.Notes?.Trim() ?? "Importación histórica"
                    }, ct);
                }
            }
            // CASO 3 — snapshot consolidado: sin IT de aporte ni movimiento de cuenta.

            // Movimiento de cuenta solo en alta normal (caso 1).
            if (_accountService != null && !dto.IsHistoricalImport)
            {
                await _accountService.GetOrCreateDefaultAsync(
                    userId, FinancialAccountType.Investment, ct);
                var initial = investment.Contributions.Single();
                await _accountService.SyncTransferAsync(
                    userId, FinancialAccountType.Cash,
                    FinancialAccountType.Investment, initial.Amount,
                    initial.ContributionDate, "investment-contribution",
                    initial.Id, $"Compra: {investment.Name}", ct);
            }

            return MapToResponseDto(investment);
        }, cancellationToken);
    }

    public async Task<InvestmentResponseDto> UpdateAsync(
        Guid id, Guid userId, InvestmentUpdateDto dto,
        CancellationToken cancellationToken = default)
    {
        var investment = await _investmentRepository.GetByIdAsync(
            id, cancellationToken);
        if (investment == null || investment.UserId != userId || investment.IsDeleted)
            throw new NotFoundException("Inversión", id);

        investment.Name = dto.Name.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Type))
            investment.Type = Enum.Parse<InvestmentType>(
                dto.Type.Replace("_", ""), true);
        investment.Ticker = dto.Ticker?.Trim().ToUpperInvariant();
        investment.Broker = dto.Broker?.Trim();
        if (dto.IsActive.HasValue) investment.IsActive = dto.IsActive.Value;
        investment.Notes = dto.Notes?.Trim();

        await _investmentRepository.UpdateAsync(investment, cancellationToken);
        return MapToResponseDto(investment);
    }

    public Task DeleteAsync(
        Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        return RunInTransactionAsync(async ct =>
        {
            var investment = await _investmentRepository.GetByIdAsync(id, ct);
            if (investment == null || investment.UserId != userId || investment.IsDeleted)
                throw new NotFoundException("Inversión", id);

            // Busca la transacción original para enlazar ReversalOf.
            // Null para inversiones creadas antes de este cambio (sin InvestmentTransaction).
            var originalTxn = investment.Transactions
                .Where(t => t.TransactionType is InvestmentTransactionType.Contribution
                                               or InvestmentTransactionType.HistoricalContribution
                    && t.DeletedAt == null)
                .OrderBy(t => t.TransactionDate)
                .FirstOrDefault();

            var reversalDate = DateOnly.FromDateTime(DateTime.UtcNow);

            var reversalTxn = new InvestmentTransaction
            {
                InvestmentId = investment.Id,
                TransactionType = InvestmentTransactionType.Reversal,
                Amount = -investment.CurrentValue,
                TransactionDate = reversalDate,
                ReversalOf = originalTxn?.Id,
                Notes = "Eliminación de inversión"
            };
            await _investmentRepository.AddTransactionAsync(reversalTxn, ct);

            if (_accountService != null)
            {
                await _accountService.SyncMovementAsync(
                    userId, null, FinancialAccountType.Investment,
                    -investment.CurrentValue, reversalDate,
                    "investment-deletion", investment.Id,
                    $"Eliminación: {investment.Name}", ct);
            }

            investment.DeletedAt = DateTimeOffset.UtcNow;
            await _investmentRepository.UpdateAsync(investment, ct);
            return true;
        }, cancellationToken);
    }

    public async Task<InvestmentSummaryDto> GetSummaryAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        var investments = (await _investmentRepository.GetByUserIdAsync(
            userId, cancellationToken)).Where(i => i.IsActive).ToList();
        var totalInvested = investments.Sum(i => i.InitialAmount);
        var currentValue = investments.Sum(i => i.CurrentValue);
        var totalGain = currentValue - totalInvested;

        return new InvestmentSummaryDto
        {
            TotalInvested = totalInvested,
            CurrentValue = currentValue,
            TotalGain = totalGain,
            TotalGainPercentage = totalInvested > 0
                ? Math.Round(totalGain / totalInvested * 100, 2)
                : 0,
            TotalDividends = investments.SelectMany(i => i.Records)
                .Sum(r => r.Dividends),
            ByType = investments.GroupBy(i => i.Type)
                .Select(g => new InvestmentByTypeDto
                {
                    Type = g.Key.ToString().ToLowerInvariant(),
                    CurrentValue = g.Sum(i => i.CurrentValue),
                    Percentage = currentValue > 0
                        ? Math.Round(g.Sum(i => i.CurrentValue) / currentValue * 100, 2)
                        : 0
                })
                .OrderByDescending(x => x.CurrentValue)
                .ToList()
        };
    }

    public async Task<IEnumerable<InvestmentRecordResponseDto>> GetRecordsAsync(
        Guid investmentId, Guid userId,
        CancellationToken cancellationToken = default)
    {
        var investment = await _investmentRepository.GetByIdAsync(
            investmentId, cancellationToken);
        if (investment == null || investment.UserId != userId || investment.IsDeleted)
            throw new NotFoundException("Inversión", investmentId);
        return investment.Records
            .OrderByDescending(r => r.RecordDate)
            .Select(MapRecordToResponseDto);
    }

    public async Task<InvestmentRecordResponseDto> AddRecordAsync(
        Guid investmentId, Guid userId, InvestmentRecordCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        var investment = await _investmentRepository.GetByIdAsync(
            investmentId, cancellationToken);
        if (investment == null || investment.UserId != userId || investment.IsDeleted)
            throw new NotFoundException("Inversión", investmentId);

        if (_accountService != null)
            await _accountService.GetOrCreateDefaultAsync(
                userId, FinancialAccountType.Investment, cancellationToken);

        var previousValue = investment.CurrentValue;
        var record = new InvestmentRecord
        {
            InvestmentId = investmentId,
            RecordDate = dto.RecordDate,
            Value = dto.Value,
            Dividends = dto.Dividends,
            Notes = dto.Notes?.Trim()
        };
        investment.Records.Add(record);
        investment.CurrentValue = dto.Value;
        await _investmentRepository.UpdateAsync(investment, cancellationToken);

        if (_accountService != null)
        {
            await _accountService.SyncMovementAsync(
                userId, null, FinancialAccountType.Investment,
                dto.Value - previousValue, dto.RecordDate,
                "investment-valuation", record.Id,
                $"Valorización: {investment.Name}", cancellationToken);
            if (dto.Dividends > 0)
            {
                await _accountService.SyncMovementAsync(
                    userId, null, FinancialAccountType.Cash, dto.Dividends,
                    dto.RecordDate, "investment-dividend", record.Id,
                    $"Dividendo: {investment.Name}", cancellationToken);
            }
        }

        return MapRecordToResponseDto(record);
    }

    public async Task<InvestmentContributionResponseDto> AddContributionAsync(
        Guid investmentId, Guid userId, InvestmentContributionCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        var investment = await _investmentRepository.GetByIdAsync(
            investmentId, cancellationToken);
        if (investment == null || investment.UserId != userId || investment.IsDeleted)
            throw new NotFoundException("Inversión", investmentId);
        if (dto.Amount <= 0)
            throw new DomainException(
                "INVALID_CONTRIBUTION_AMOUNT",
                "El monto del aporte debe ser mayor a 0");

        if (_accountService != null)
        {
            await _accountService.GetOrCreateDefaultAsync(
                userId, FinancialAccountType.Cash, cancellationToken);
            await _accountService.GetOrCreateDefaultAsync(
                userId, FinancialAccountType.Investment, cancellationToken);
        }
        investment.InitialAmount += dto.Amount;
        // CurrentValue NO se toca: la valoración de mercado es un evento separado (AddRecordAsync).
        var contribution = new InvestmentContribution
        {
            InvestmentId = investmentId,
            ContributionDate = dto.ContributionDate
                ?? DateOnly.FromDateTime(DateTime.UtcNow),
            Amount = dto.Amount,
            Notes = dto.Notes?.Trim()
        };
        await _investmentRepository.AddContributionAsync(
            contribution, cancellationToken);

        if (_accountService != null)
        {
            await _accountService.SyncTransferAsync(
                userId, FinancialAccountType.Cash,
                FinancialAccountType.Investment, contribution.Amount,
                contribution.ContributionDate, "investment-contribution",
                contribution.Id, $"Aporte: {investment.Name}", cancellationToken);
        }

        return new InvestmentContributionResponseDto
        {
            Id = contribution.Id,
            ContributionDate = contribution.ContributionDate,
            Amount = contribution.Amount,
            Notes = contribution.Notes,
            CreatedAt = contribution.CreatedAt
        };
    }

    public Task<InvestmentWithdrawalResponseDto> WithdrawAsync(
        Guid investmentId, Guid userId, InvestmentWithdrawalDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto.WithdrawalAmount <= 0)
            throw new DomainException("INVALID_WITHDRAWAL_AMOUNT",
                "El monto del retiro debe ser mayor que cero.");
        if (dto.CapitalReturned < 0 || dto.CapitalReturned > dto.WithdrawalAmount)
            throw new DomainException("INVALID_CAPITAL_RETURNED",
                "El capital recuperado debe estar entre cero y el monto del retiro.");
        if (dto.Fee < 0 || dto.Fee >= dto.WithdrawalAmount)
            throw new DomainException("INVALID_FEE",
                "La comisión no puede ser negativa ni mayor o igual al monto del retiro.");
        if (dto.CapitalReturned + dto.Fee > dto.WithdrawalAmount)
            throw new DomainException("WITHDRAWAL_BREAKDOWN_EXCEEDS_AMOUNT",
                "La suma de capital recuperado y comisión no puede superar el monto del retiro.");

        return RunInTransactionAsync(async ct =>
        {
            var investment = await _investmentRepository.GetByIdAsync(investmentId, ct);
            if (investment == null || investment.UserId != userId || investment.IsDeleted)
                throw new NotFoundException("Inversión", investmentId);
            if (!investment.IsActive)
                throw new DomainException("INACTIVE_INVESTMENT",
                    "No se puede retirar de una inversión inactiva.");
            if (dto.WithdrawalAmount > investment.CurrentValue)
                throw new DomainException("WITHDRAWAL_EXCEEDS_CURRENT_VALUE",
                    "El monto del retiro supera el valor actual de la inversión.");
            if (dto.CapitalReturned > investment.InitialAmount)
                throw new DomainException("CAPITAL_RETURNED_EXCEEDS_CONTRIBUTED",
                    "El capital recuperado supera el capital aportado acumulado.");

            var netCash = dto.WithdrawalAmount - dto.Fee;
            var realizedGain = dto.WithdrawalAmount - dto.CapitalReturned - dto.Fee;

            await _investmentRepository.AddTransactionAsync(new InvestmentTransaction
            {
                InvestmentId = investmentId,
                TransactionType = InvestmentTransactionType.Withdrawal,
                Amount = dto.WithdrawalAmount,
                TransactionDate = dto.WithdrawalDate,
                IsHistorical = false,
                Notes = dto.Notes?.Trim()
            }, ct);

            if (dto.Fee > 0)
            {
                await _investmentRepository.AddTransactionAsync(new InvestmentTransaction
                {
                    InvestmentId = investmentId,
                    TransactionType = InvestmentTransactionType.Fee,
                    Amount = dto.Fee,
                    TransactionDate = dto.WithdrawalDate,
                    IsHistorical = false,
                    Notes = $"Comisión de retiro: {investment.Name}"
                }, ct);
            }

            investment.InitialAmount -= dto.CapitalReturned;
            investment.CurrentValue -= dto.WithdrawalAmount;
            await _investmentRepository.UpdateAsync(investment, ct);

            if (_accountService != null)
            {
                await _accountService.SyncTransferAsync(
                    userId, FinancialAccountType.Investment, FinancialAccountType.Cash,
                    netCash, dto.WithdrawalDate,
                    "investment-withdrawal", investmentId,
                    $"Retiro: {investment.Name}", ct);
            }

            return new InvestmentWithdrawalResponseDto
            {
                InvestmentId = investmentId,
                WithdrawalAmount = dto.WithdrawalAmount,
                CapitalReturned = dto.CapitalReturned,
                RealizedGain = realizedGain,
                Fee = dto.Fee,
                NetCashReceived = netCash,
                WithdrawalDate = dto.WithdrawalDate,
                RemainingContributedCapital = investment.InitialAmount,
                RemainingCurrentValue = investment.CurrentValue
            };
        }, cancellationToken);
    }

    private Task<T> RunInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
        => _unitOfWork == null
            ? action(cancellationToken)
            : _unitOfWork.ExecuteInTransactionAsync(action, cancellationToken);

    private static InvestmentResponseDto MapToResponseDto(Investment investment)
    {
        var unrealizedGainLoss = investment.CurrentValue - investment.InitialAmount;
        var unrealizedGainLossPct = investment.InitialAmount > 0
            ? Math.Round(unrealizedGainLoss / investment.InitialAmount * 100, 2)
            : 0m;
        return new InvestmentResponseDto
        {
            Id = investment.Id,
            Name = investment.Name,
            Type = investment.Type.ToString().ToLowerInvariant(),
            Ticker = investment.Ticker,
            Broker = investment.Broker,
            ContributedCapital = investment.InitialAmount,
            CurrentValue = investment.CurrentValue,
            UnrealizedGainLoss = unrealizedGainLoss,
            UnrealizedGainLossPercentage = unrealizedGainLossPct,
            // aliases de compatibilidad
            InitialAmount = investment.InitialAmount,
            GainLoss = unrealizedGainLoss,
            GainLossPercentage = unrealizedGainLossPct,
            PurchaseDate = investment.PurchaseDate,
            IsActive = investment.IsActive,
            Notes = investment.Notes,
            CreatedAt = investment.CreatedAt
        };
    }

    private static InvestmentRecordResponseDto MapRecordToResponseDto(
        InvestmentRecord record) => new()
    {
        Id = record.Id,
        RecordDate = record.RecordDate,
        Value = record.Value,
        Dividends = record.Dividends,
        Notes = record.Notes,
        CreatedAt = record.CreatedAt
    };
}
