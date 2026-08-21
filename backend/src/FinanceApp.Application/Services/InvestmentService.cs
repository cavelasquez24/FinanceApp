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
            if (_accountService != null)
                await _accountService.GetOrCreateDefaultAsync(
                    userId, FinancialAccountType.Investment, ct);

            var investment = new Investment
            {
                UserId = userId,
                Name = dto.Name.Trim(),
                Type = Enum.Parse<InvestmentType>(
                    dto.Type.Replace("_", ""), true),
                Ticker = dto.Ticker?.Trim().ToUpperInvariant(),
                Broker = dto.Broker?.Trim(),
                InitialAmount = dto.InitialAmount,
                CurrentValue = dto.IsHistoricalImport
                    ? dto.CurrentValue ?? dto.InitialAmount
                    : dto.InitialAmount,
                PurchaseDate = dto.PurchaseDate,
                Notes = dto.Notes?.Trim(),
                IsActive = true
            };

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

            var openingTxn = new InvestmentTransaction
            {
                InvestmentId = investment.Id,
                TransactionType = dto.IsHistoricalImport
                    ? InvestmentTransactionType.HistoricalContribution
                    : InvestmentTransactionType.Contribution,
                Amount = dto.IsHistoricalImport
                    ? investment.CurrentValue
                    : dto.InitialAmount,
                TransactionDate = dto.PurchaseDate,
                IsHistorical = dto.IsHistoricalImport,
                Notes = dto.IsHistoricalImport ? "Importación histórica" : "Compra inicial"
            };
            await _investmentRepository.AddTransactionAsync(openingTxn, ct);

            if (_accountService != null)
            {
                if (dto.IsHistoricalImport)
                {
                    await _accountService.SyncMovementAsync(
                        userId, null, FinancialAccountType.Investment,
                        investment.CurrentValue, investment.PurchaseDate,
                        "investment-opening", investment.Id,
                        $"Importación: {investment.Name}", ct);
                }
                else
                {
                    var initial = investment.Contributions.Single();
                    await _accountService.SyncTransferAsync(
                        userId, FinancialAccountType.Cash,
                        FinancialAccountType.Investment, initial.Amount,
                        initial.ContributionDate, "investment-contribution",
                        initial.Id, $"Compra: {investment.Name}", ct);
                }
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
