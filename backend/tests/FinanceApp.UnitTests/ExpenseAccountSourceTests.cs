using FinanceApp.Application.DTOs.Account;
using FinanceApp.Application.DTOs.Common;
using FinanceApp.Application.DTOs.CreditCard;
using FinanceApp.Application.DTOs.Expense;
using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.Interfaces.Repositories;
using FinanceApp.Domain.Models;

namespace FinanceApp.UnitTests;

public class ExpenseAccountSourceTests
{
    [Fact]
    public async Task Create_UsesExplicitCashAccount_AndCreatesSingleDebitMovement()
    {
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var accounts = new RecordingAccountService();
        var repository = new FakeExpenseRepository();
        var service = CreateService(repository, accounts);

        await service.CreateAsync(userId, NewCreateDto(accountId, 20m, new DateOnly(2026, 8, 16)));

        var expense = Assert.Single(repository.Items);
        Assert.Equal(accountId, expense.AccountId);
        Assert.Single(accounts.ValidatedAccounts);
        Assert.Equal((userId, accountId, FinancialAccountType.Cash), accounts.ValidatedAccounts[0]);
        var movement = Assert.Single(accounts.Movements);
        Assert.Equal(accountId, movement.AccountId);
        Assert.Equal(-20m, movement.SignedAmount);
        Assert.Equal("expense", movement.SourceType);
        Assert.Equal(expense.Id, movement.SourceId);
    }

    [Fact]
    public async Task Update_ChangingAccountAmountAndDate_SynchronizesTheSameSource()
    {
        var userId = Guid.NewGuid();
        var oldAccount = Guid.NewGuid();
        var newAccount = Guid.NewGuid();
        var expense = NewExpense(userId, oldAccount, 20m, new DateOnly(2026, 8, 1));
        var accounts = new RecordingAccountService();
        var service = CreateService(new FakeExpenseRepository(expense), accounts);

        await service.UpdateAsync(expense.Id, userId, new ExpenseUpdateDto
        {
            CategoryId = expense.CategoryId,
            AccountId = newAccount,
            Amount = 35m,
            Date = new DateOnly(2026, 8, 15),
            PaymentMethod = "debit_card",
            TagIds = []
        });

        Assert.Equal(newAccount, expense.AccountId);
        Assert.Equal(35m, expense.Amount);
        Assert.Equal(new DateOnly(2026, 8, 15), expense.Date);
        var movement = Assert.Single(accounts.Movements);
        Assert.Equal(newAccount, movement.AccountId);
        Assert.Equal(-35m, movement.SignedAmount);
        Assert.Equal(expense.Id, movement.SourceId);
    }

    [Fact]
    public async Task Delete_ReversesTheExistingExpenseMovement()
    {
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var expense = NewExpense(userId, accountId, 20m, new DateOnly(2026, 8, 1));
        var accounts = new RecordingAccountService();
        var service = CreateService(new FakeExpenseRepository(expense), accounts);

        await service.DeleteAsync(expense.Id, userId);

        Assert.NotNull(expense.DeletedAt);
        var movement = Assert.Single(accounts.Movements);
        Assert.Equal(accountId, movement.AccountId);
        Assert.Equal(0m, movement.SignedAmount);
        Assert.Equal(expense.Id, movement.SourceId);
    }

    [Fact]
    public async Task Update_RejectsExpenseFromAnotherUser()
    {
        var ownerId = Guid.NewGuid();
        var expense = NewExpense(ownerId, Guid.NewGuid(), 20m, new DateOnly(2026, 8, 1));
        var accounts = new RecordingAccountService();
        var service = CreateService(new FakeExpenseRepository(expense), accounts);

        await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(
            expense.Id, Guid.NewGuid(), new ExpenseUpdateDto
            {
                CategoryId = expense.CategoryId,
                AccountId = expense.AccountId,
                Amount = 20m,
                Date = expense.Date,
                PaymentMethod = "cash",
                TagIds = []
            }));

        Assert.Empty(accounts.Movements);
        Assert.Empty(accounts.ValidatedAccounts);
    }

    [Fact]
    public async Task Create_RejectsAnInactiveOrForeignAccount()
    {
        var accounts = new RecordingAccountService
        {
            AvailableBalanceException = new DomainException("INVALID_ACCOUNT", "La cuenta seleccionada no está disponible.")
        };
        var service = CreateService(new FakeExpenseRepository(), accounts);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.CreateAsync(Guid.NewGuid(), NewCreateDto(Guid.NewGuid(), 20m, new DateOnly(2026, 8, 16))));

        Assert.Equal("INVALID_ACCOUNT", exception.Code);
        Assert.Empty(accounts.Movements);
    }

    [Fact]
    public async Task Create_CreditCardPurchase_CreatesExpenseAndLiabilityWithoutCashMovement()
    {
        var userId = Guid.NewGuid();
        var cardId = Guid.NewGuid();
        var accounts = new RecordingAccountService();
        var cards = new RecordingCreditCardService();
        var repository = new FakeExpenseRepository();
        var service = CreateService(repository, accounts, cards);
        var dto = NewCreateDto(Guid.NewGuid(), 20m, new DateOnly(2026, 8, 16));
        dto.AccountId = null;
        dto.CreditCardId = cardId;
        dto.IdempotencyKey = Guid.NewGuid();
        dto.PaymentMethod = "credit_card";

        await service.CreateAsync(userId, dto);

        var expense = Assert.Single(repository.Items);
        Assert.Null(expense.AccountId);
        Assert.Equal(cardId, expense.CreditCardId);
        Assert.Empty(accounts.ValidatedAccounts);
        Assert.Empty(accounts.Movements);
        var liability = Assert.Single(cards.SyncedExpenses);
        Assert.Equal(20m, liability.Amount);
        Assert.Equal(expense.Id, liability.ExpenseId);
        Assert.Equal(CreditCardTransactionType.Purchase, liability.Type);
    }

    private static ExpenseService CreateService(FakeExpenseRepository expenses, RecordingAccountService accounts, RecordingCreditCardService? cards = null) =>
        new(expenses, new EmptyTagRepository(), accounts, cards ?? new RecordingCreditCardService(),
            new PassThroughUnitOfWork());

    private static ExpenseCreateDto NewCreateDto(Guid accountId, decimal amount, DateOnly date) => new()
    {
        CategoryId = Guid.NewGuid(),
        AccountId = accountId,
        Amount = amount,
        Date = date,
        PaymentMethod = "cash",
        TagIds = []
    };

    private static Expense NewExpense(Guid userId, Guid accountId, decimal amount, DateOnly date) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        CategoryId = Guid.NewGuid(),
        AccountId = accountId,
        Amount = amount,
        Date = date,
        PaymentMethod = PaymentMethod.Cash
    };

    private sealed class RecordingAccountService : IFinancialAccountService
    {
        public DomainException? AvailableBalanceException { get; init; }
        public List<(Guid UserId, Guid AccountId, FinancialAccountType Type)> ValidatedAccounts { get; } = [];
        public List<Movement> Movements { get; } = [];

        public Task<decimal> GetAvailableBalanceAsync(Guid userId, Guid? accountId, FinancialAccountType fallbackType, CancellationToken cancellationToken = default)
        {
            if (AvailableBalanceException is not null)
                throw AvailableBalanceException;
            ValidatedAccounts.Add((userId, accountId!.Value, fallbackType));
            return Task.FromResult(0m);
        }

        public Task SyncMovementAsync(Guid userId, Guid? accountId, FinancialAccountType fallbackType, decimal signedAmount, DateOnly date, string sourceType, Guid sourceId, string description, CancellationToken cancellationToken = default)
        {
            Movements.Add(new Movement(accountId, signedAmount, date, sourceType, sourceId));
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<FinancialAccountResponseDto>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<FinancialAccountResponseDto>>([]);
        public Task<IReadOnlyList<AccountTransactionResponseDto>> GetRecentTransactionsAsync(Guid userId, int count, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<AccountTransactionResponseDto>>([]);
        public Task<FinancialAccountResponseDto> CreateAsync(Guid userId, FinancialAccountCreateDto dto, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<FinancialAccountResponseDto> UpdateAsync(Guid id, Guid userId, FinancialAccountUpdateDto dto, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<FinancialAccountResponseDto> GetOrCreateDefaultAsync(Guid userId, FinancialAccountType type, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<AccountTransferResponseDto> TransferAsync(Guid userId, AccountTransferCreateDto dto, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SyncTransferAsync(Guid userId, FinancialAccountType fromType, FinancialAccountType toType, decimal amount, DateOnly date, string sourceType, Guid sourceId, string description, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SyncTransferBetweenAccountsAsync(Guid userId, Guid? fromAccountId, FinancialAccountType fromFallbackType, Guid? toAccountId, FinancialAccountType toFallbackType, decimal amount, DateOnly date, string sourceType, Guid sourceId, string description, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed record Movement(Guid? AccountId, decimal SignedAmount, DateOnly Date, string SourceType, Guid SourceId);

    private sealed class FakeExpenseRepository : IExpenseRepository
    {
        public List<Expense> Items { get; } = [];
        public FakeExpenseRepository(Expense? expense = null) { if (expense is not null) Items.Add(expense); }
        public Task<Expense?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Items.SingleOrDefault(x => x.Id == id));
        public Task<Expense?> GetByIdempotencyKeyAsync(Guid userId, Guid idempotencyKey, CancellationToken cancellationToken = default) => Task.FromResult(Items.SingleOrDefault(x => x.UserId == userId && x.IdempotencyKey == idempotencyKey));
        public Task<IEnumerable<Expense>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<Expense>>(Items);
        public Task<Expense> CreateAsync(Expense entity, CancellationToken cancellationToken = default) { entity.Id = entity.Id == Guid.Empty ? Guid.NewGuid() : entity.Id; Items.Add(entity); return Task.FromResult(entity); }
        public Task<Expense> UpdateAsync(Expense entity, CancellationToken cancellationToken = default) => Task.FromResult(entity);
        public Task DeleteAsync(Expense entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<(IEnumerable<Expense> Items, int TotalCount)> GetByUserIdAsync(Guid userId, int page, int pageSize, Guid? categoryId = null, DateOnly? startDate = null, DateOnly? endDate = null, CancellationToken cancellationToken = default) => Task.FromResult(((IEnumerable<Expense>)[], 0));
        public Task<(IEnumerable<Expense> Items, int TotalCount)> GetFilteredAsync(Guid userId, ExpenseQueryOptions options, CancellationToken cancellationToken = default) => Task.FromResult(((IEnumerable<Expense>)[], 0));
        public Task<decimal> GetTotalByUserAndPeriodAsync(Guid userId, int month, int year, CancellationToken cancellationToken = default) => Task.FromResult(0m);
        public Task<IEnumerable<(Guid CategoryId, string CategoryName, string CategoryColor, decimal Total)>> GetByCategoryAsync(Guid userId, int month, int year, CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<(Guid, string, string, decimal)>>([]);
        public Task<decimal> GetTotalByDateRangeAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default) => Task.FromResult(0m);
        public Task<IEnumerable<(Guid CategoryId, string CategoryName, string CategoryColor, decimal Total)>> GetByCategoryByDateRangeAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<(Guid, string, string, decimal)>>([]);
    }

    private sealed class EmptyTagRepository : ITagRepository
    {
        public Task<Tag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Tag?>(null);
        public Task<IEnumerable<Tag>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<Tag>>([]);
        public Task<Tag> CreateAsync(Tag entity, CancellationToken cancellationToken = default) => Task.FromResult(entity);
        public Task<Tag> UpdateAsync(Tag entity, CancellationToken cancellationToken = default) => Task.FromResult(entity);
        public Task DeleteAsync(Tag entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<Tag>> GetByUserAsync(Guid userId, string? search = null, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Tag>>([]);
        public Task<IReadOnlyList<Tag>> GetActiveByIdsAsync(Guid userId, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Tag>>([]);
        public Task<int> CountActiveAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<bool> ExistsActiveAsync(Guid userId, string normalizedName, Guid? excludingId = null, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task MergeAsync(Tag source, Tag target, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<TagExpenseMetric>> GetExpenseReportAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<TagExpenseMetric>>([]);
        public Task<(int TotalExpenses, int TaggedExpenses)> GetCoverageAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default) => Task.FromResult((0, 0));
    }
}
