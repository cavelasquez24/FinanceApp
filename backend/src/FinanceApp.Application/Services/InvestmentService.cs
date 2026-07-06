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
            CurrentValue = dto.CurrentValue,
            PurchaseDate = dto.PurchaseDate,
            Notes = dto.Notes?.Trim(),
            IsActive = true
        };

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
        investment.Ticker = dto.Ticker?.Trim().ToUpper();
        investment.Broker = dto.Broker?.Trim();
        investment.CurrentValue = dto.CurrentValue;
        investment.IsActive = dto.IsActive;
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