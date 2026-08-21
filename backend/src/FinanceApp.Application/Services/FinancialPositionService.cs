using FinanceApp.Application.DTOs.Dashboard;
using FinanceApp.Application.Interfaces;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.Interfaces.Repositories;

namespace FinanceApp.Application.Services;

/// <summary>
/// Única fuente de verdad para patrimonio: activos reales menos pasivos externos.
/// Metas y restauraciones internas se muestran como contexto, pero no se suman ni restan.
/// </summary>
public class FinancialPositionService : IFinancialPositionService
{
    private readonly IFinancialAccountRepository _accountRepository;
    private readonly IInvestmentRepository _investmentRepository;
    private readonly IDebtRepository _debtRepository;
    private readonly ICreditCardRepository _creditCardRepository;
    private readonly ISavingsGoalRepository _savingsGoalRepository;

    public FinancialPositionService(
        IFinancialAccountRepository accountRepository,
        IInvestmentRepository investmentRepository,
        IDebtRepository debtRepository,
        ICreditCardRepository creditCardRepository,
        ISavingsGoalRepository savingsGoalRepository)
    {
        _accountRepository = accountRepository;
        _investmentRepository = investmentRepository;
        _debtRepository = debtRepository;
        _creditCardRepository = creditCardRepository;
        _savingsGoalRepository = savingsGoalRepository;
    }

    public async Task<FinancialPositionDto> GetAsync(
        Guid userId,
        DateOnly? asOf = null,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        if (asOf.HasValue && asOf.Value != today)
        {
            throw new DomainException(
                "HISTORICAL_FINANCIAL_POSITION_NOT_AVAILABLE",
                "El patrimonio histórico todavía no está disponible. El corte soportado es el snapshot actual.");
        }

        // Consultas secuenciales: los repositorios comparten el mismo DbContext scoped.
        var accounts = await _accountRepository.GetByUserIdAsync(userId, cancellationToken);
        var (openingBalances, accountAdjustments) =
            await _accountRepository.GetOpeningAndAdjustmentTotalsAsync(userId, cancellationToken);
        var investments = await _investmentRepository.GetByUserIdAsync(userId, cancellationToken);
        var debts = await _debtRepository.GetByUserIdAsync(userId, cancellationToken);
        var cards = await _creditCardRepository.GetByUserIdAsync(userId, cancellationToken);
        var goalAllocations = await _savingsGoalRepository.GetTotalSavedAsync(userId, cancellationToken);

        return CalculateCurrentSnapshot(
            today,
            accounts,
            investments,
            debts,
            cards,
            goalAllocations,
            openingBalances,
            accountAdjustments);
    }

    public static FinancialPositionDto CalculateCurrentSnapshot(
        DateOnly asOf,
        IEnumerable<FinancialAccount> accounts,
        IEnumerable<Investment> investments,
        IEnumerable<Debt> debts,
        IEnumerable<CreditCard> cards,
        decimal savingsGoalAllocations,
        decimal accountOpeningBalances = 0m,
        decimal accountAdjustments = 0m)
    {
        var accountList = accounts.Where(a => !a.IsDeleted).ToList();
        var investmentList = investments.Where(i => !i.IsDeleted).ToList();
        var debtList = debts.Where(d => !d.IsDeleted).ToList();
        var cardList = cards.Where(c => !c.IsDeleted).ToList();
        var warnings = new List<FinancialPositionWarningDto>();

        var cashAccounts = Round(accountList
            .Where(a => a.Type == FinancialAccountType.Cash)
            .Sum(a => a.CurrentBalance));
        var savingsAccounts = Round(accountList
            .Where(a => a.Type == FinancialAccountType.Savings)
            .Sum(a => a.CurrentBalance));
        var investmentAccountBalance = Round(accountList
            .Where(a => a.Type == FinancialAccountType.Investment)
            .Sum(a => a.CurrentBalance));

        var investmentPositions = Round(investmentList.Sum(i => i.CurrentValue));
        var investmentCostBasis = Round(investmentList.Sum(i => i.InitialAmount));
        var investmentUnrealizedGainLoss = Round(investmentPositions - investmentCostBasis);
        // Las posiciones son la fuente canónica del valor invertido.
        // El efectivo real en broker se registrará explícitamente (#7).
        // El exceso de la cuenta agregada sobre posiciones no entra al patrimonio
        // para evitar inflar activos con saldos fantasma.
        var uninvestedInvestmentCash = 0m;
        var totalInvestments = investmentPositions;

        var debtLiabilities = Round(debtList
            .Where(d => d.CurrentBalance > 0)
            .Sum(d => d.CurrentBalance));
        var cardLiabilities = Round(cardList
            .Where(c => c.CurrentBalance > 0)
            .Sum(c => c.CurrentBalance));
        var cardCredits = Round(cardList
            .Where(c => c.CurrentBalance < 0)
            .Sum(c => Math.Abs(c.CurrentBalance)));

        var totalAssets = Round(
            cashAccounts
            + savingsAccounts
            + totalInvestments
            + cardCredits);
        var totalLiabilities = Round(debtLiabilities + cardLiabilities);
        var netWorth = Round(totalAssets - totalLiabilities);

        if (accountList.Any(a => !a.IsActive && a.CurrentBalance != 0))
        {
            warnings.Add(Warning(
                "INACTIVE_ACCOUNT_WITH_BALANCE",
                "Existen cuentas inactivas con saldo. Se incluyen en el patrimonio hasta que se cierren o transfieran explícitamente."));
        }

        var investmentDifference = Round(investmentAccountBalance - investmentPositions);
        if (investmentDifference != 0)
        {
            warnings.Add(Warning(
                "INVESTMENT_LEDGER_DIFFERENCE",
                investmentDifference > 0
                    ? "La cuenta agregada de inversión supera el valor de las posiciones; el exceso se cuenta como efectivo no invertido."
                    : "El valor de las posiciones supera la cuenta agregada de inversión; las posiciones son la fuente de valoración y la diferencia requiere revisión."));
        }

        if (investmentList.Any(i => !i.IsActive && i.CurrentValue != 0))
        {
            warnings.Add(Warning(
                "INACTIVE_INVESTMENT_WITH_VALUE",
                "Existen inversiones inactivas con valor distinto de cero. Se incluyen mientras conserven valor económico y deben revisarse."));
        }

        if (debtList.Any(d => !d.IsActive && d.CurrentBalance > 0))
        {
            warnings.Add(Warning(
                "INACTIVE_DEBT_WITH_BALANCE",
                "Existen deudas inactivas con saldo pendiente. Se incluyen como pasivo hasta que su saldo llegue a cero."));
        }

        if (cardList.Any(c => !c.IsActive && c.CurrentBalance != 0))
        {
            warnings.Add(Warning(
                "INACTIVE_CARD_WITH_BALANCE",
                "Existen tarjetas inactivas con deuda o saldo a favor. Se incluyen en el patrimonio."));
        }

        if (Round(savingsGoalAllocations) > savingsAccounts)
        {
            warnings.Add(Warning(
                "SAVINGS_ALLOCATIONS_EXCEED_BACKING",
                "Las asignaciones de metas superan el saldo de cuentas de ahorro que las respalda."));
        }

        warnings.Add(Warning(
            "CURRENT_SNAPSHOT_ONLY",
            "El desglose corresponde a saldos actuales; el historial aún no permite reconstruir todos los componentes a una fecha pasada."));

        return new FinancialPositionDto
        {
            AsOf = asOf,
            HistoricalSnapshotsSupported = false,
            Assets = new FinancialPositionAssetsDto
            {
                CashAccounts = cashAccounts,
                SavingsAccounts = savingsAccounts,
                InvestmentPositions = investmentPositions,
                AccountOpeningBalances = Round(accountOpeningBalances),
                AccountAdjustments = Round(accountAdjustments),
                InvestmentCostBasis = investmentCostBasis,
                InvestmentUnrealizedGainLoss = investmentUnrealizedGainLoss,
                InvestmentAccountBalance = investmentAccountBalance,
                UninvestedInvestmentCash = uninvestedInvestmentCash,
                Investments = totalInvestments,
                CreditCardCredits = cardCredits,
                Total = totalAssets
            },
            Liabilities = new FinancialPositionLiabilitiesDto
            {
                Debts = debtLiabilities,
                CreditCards = cardLiabilities,
                Total = totalLiabilities
            },
            SavingsGoalAllocations = Round(savingsGoalAllocations),
            NetWorth = netWorth,
            Warnings = warnings
        };
    }

    private static decimal Round(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static FinancialPositionWarningDto Warning(string code, string message) => new()
    {
        Code = code,
        Message = message
    };
}
