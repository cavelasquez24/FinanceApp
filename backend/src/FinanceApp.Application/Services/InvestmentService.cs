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

    public InvestmentService(IInvestmentRepository investmentRepository)
    {
        _investmentRepository = investmentRepository;
    }

    public async Task<IEnumerable<InvestmentResponseDto>> GetAllAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var investments = await _investmentRepository
            .GetByUserIdAsync(userId, cancellationToken);
        return investments.Select(MapToResponseDto);
    }

    public async Task<InvestmentResponseDto> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var investment = await _investmentRepository.GetByIdAsync(id, cancellationToken);

        if (investment == null || investment.UserId != userId || investment.IsDeleted)
            throw new NotFoundException("Inversión", id);

        return MapToResponseDto(investment);
    }

    public async Task<InvestmentResponseDto> CreateAsync(
        Guid userId,
        InvestmentCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        var type = Enum.Parse<InvestmentType>(
            dto.Type.Replace("_", ""), ignoreCase: true);

        var investment = new Investment
        {
            UserId = userId,
            Name = dto.Name.Trim(),
            Type = type,
            Ticker = dto.Ticker?.Trim().ToUpper(),
            Broker = dto.Broker?.Trim(),
            InitialAmount = dto.InitialAmount,
            CurrentValue = dto.IsHistoricalImport
                ? dto.CurrentValue ?? dto.InitialAmount
                : dto.InitialAmount,
            PurchaseDate = dto.PurchaseDate,
            Notes = dto.Notes?.Trim(),
            IsActive = true
        };
        // A new purchase consumes cash in its cycle. A historical import only
        // establishes the opening portfolio snapshot.
        if (!dto.IsHistoricalImport)
        {
            investment.Contributions.Add(new InvestmentContribution
            {
                ContributionDate = dto.PurchaseDate,
                Amount = dto.InitialAmount,
                Notes = "Compra inicial"
            });
        }

        await _investmentRepository.CreateAsync(investment, cancellationToken);
        return MapToResponseDto(investment);
    }

    public async Task<InvestmentResponseDto> UpdateAsync(
        Guid id,
        Guid userId,
        InvestmentUpdateDto dto,
        CancellationToken cancellationToken = default)
    {
        var investment = await _investmentRepository.GetByIdAsync(id, cancellationToken);

        if (investment == null || investment.UserId != userId || investment.IsDeleted)
            throw new NotFoundException("Inversión", id);

        investment.Name = dto.Name.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Type))
        {
            investment.Type = Enum.Parse<InvestmentType>(
                dto.Type.Replace("_", ""), ignoreCase: true);
        }
        investment.Ticker = dto.Ticker?.Trim().ToUpper();
        investment.Broker = dto.Broker?.Trim();
        if (dto.IsActive.HasValue)
            investment.IsActive = dto.IsActive.Value;
        investment.Notes = dto.Notes?.Trim();

        await _investmentRepository.UpdateAsync(investment, cancellationToken);
        return MapToResponseDto(investment);
    }

    public async Task DeleteAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var investment = await _investmentRepository.GetByIdAsync(id, cancellationToken);

        if (investment == null || investment.UserId != userId || investment.IsDeleted)
            throw new NotFoundException("Inversión", id);

        investment.DeletedAt = DateTimeOffset.UtcNow;
        await _investmentRepository.UpdateAsync(investment, cancellationToken);
    }

    public async Task<InvestmentSummaryDto> GetSummaryAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var investments = (await _investmentRepository
            .GetByUserIdAsync(userId, cancellationToken))
            .Where(i => i.IsActive)
            .ToList();

        var totalInvested = investments.Sum(i => i.InitialAmount);
        var currentValue = investments.Sum(i => i.CurrentValue);
        var totalGain = currentValue - totalInvested;
        var totalDividends = investments
            .SelectMany(i => i.Records)
            .Sum(r => r.Dividends);

        // Agrupamos por tipo para el gráfico de distribución
        var byType = investments
            .GroupBy(i => i.Type)
            .Select(g => new InvestmentByTypeDto
            {
                Type = g.Key.ToString().ToLower(),
                CurrentValue = g.Sum(i => i.CurrentValue),
                Percentage = currentValue > 0
                    ? Math.Round(g.Sum(i => i.CurrentValue) / currentValue * 100, 2)
                    : 0
            })
            .OrderByDescending(x => x.CurrentValue)
            .ToList();

        return new InvestmentSummaryDto
        {
            TotalInvested = totalInvested,
            CurrentValue = currentValue,
            TotalGain = totalGain,
            TotalGainPercentage = totalInvested > 0
                ? Math.Round(totalGain / totalInvested * 100, 2)
                : 0,
            TotalDividends = totalDividends,
            ByType = byType
        };
    }

    public async Task<IEnumerable<InvestmentRecordResponseDto>> GetRecordsAsync(
        Guid investmentId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var investment = await _investmentRepository
            .GetByIdAsync(investmentId, cancellationToken);

        if (investment == null || investment.UserId != userId || investment.IsDeleted)
            throw new NotFoundException("Inversión", investmentId);

        return investment.Records
            .OrderByDescending(r => r.RecordDate)
            .Select(MapRecordToResponseDto);
    }

    public async Task<InvestmentRecordResponseDto> AddRecordAsync(
        Guid investmentId,
        Guid userId,
        InvestmentRecordCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        var investment = await _investmentRepository
            .GetByIdAsync(investmentId, cancellationToken);

        if (investment == null || investment.UserId != userId || investment.IsDeleted)
            throw new NotFoundException("Inversión", investmentId);

        var record = new InvestmentRecord
        {
            InvestmentId = investmentId,
            RecordDate = dto.RecordDate,
            Value = dto.Value,
            Dividends = dto.Dividends,
            Notes = dto.Notes?.Trim()
        };

        investment.Records.Add(record);

        // Actualizamos el valor actual de la inversión con el último registro
        investment.CurrentValue = dto.Value;

        await _investmentRepository.UpdateAsync(investment, cancellationToken);

        return MapRecordToResponseDto(record);
    }

    public async Task<InvestmentContributionResponseDto> AddContributionAsync(
        Guid investmentId,
        Guid userId,
        InvestmentContributionCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        var investment = await _investmentRepository
            .GetByIdAsync(investmentId, cancellationToken);

        if (investment == null || investment.UserId != userId || investment.IsDeleted)
            throw new NotFoundException("Inversión", investmentId);

        if (dto.Amount <= 0)
            throw new DomainException(
                "INVALID_CONTRIBUTION_AMOUNT",
                "El monto del aporte debe ser mayor a 0");

        // Comprar más participación incrementa el costo base y el valor actual;
        // una valoración posterior solo modifica CurrentValue.
        investment.InitialAmount += dto.Amount;
        investment.CurrentValue += dto.Amount;

        var contribution = new InvestmentContribution
        {
            InvestmentId = investmentId,
            ContributionDate = dto.ContributionDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            Amount = dto.Amount,
            Notes = dto.Notes?.Trim()
        };

        // Investment está tracked: el cambio del agregado y el movimiento se
        // guardan juntos en el SaveChanges del repositorio.
        await _investmentRepository.AddContributionAsync(contribution, cancellationToken);

        return new InvestmentContributionResponseDto
        {
            Id = contribution.Id,
            ContributionDate = contribution.ContributionDate,
            Amount = contribution.Amount,
            Notes = contribution.Notes,
            CreatedAt = contribution.CreatedAt
        };
    }

    private static InvestmentResponseDto MapToResponseDto(Investment investment) => new()
    {
        Id = investment.Id,
        Name = investment.Name,
        Type = investment.Type.ToString().ToLower(),
        Ticker = investment.Ticker,
        Broker = investment.Broker,
        InitialAmount = investment.InitialAmount,
        CurrentValue = investment.CurrentValue,
        GainLoss = investment.GainLoss,
        GainLossPercentage = investment.GainLossPercentage,
        PurchaseDate = investment.PurchaseDate,
        IsActive = investment.IsActive,
        Notes = investment.Notes,
        CreatedAt = investment.CreatedAt
    };

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
