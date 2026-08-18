using FinanceApp.Application.DTOs.CreditCard;
using FinanceApp.Application.DTOs.Expense;
using FinanceApp.Application.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using FinanceApp.Domain.Exceptions;
using FinanceApp.Infrastructure.Persistence;
using FinanceApp.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FinanceApp.UnitTests;

public class CreditCardLedgerIntegrationTests
{
    [Fact]
    public async Task Purchase100_ThenPay100_LeavesExactly100ExpenseAndNoCardLiability()
    {
        await using var fixture = await Fixture.CreateAsync();
        var card = await fixture.CreateCardAsync("Visa");

        var purchaseRequest = new ExpenseCreateDto
        {
            CategoryId = fixture.CategoryId,
            CreditCardId = card.Id,
            IdempotencyKey = Guid.NewGuid(),
            Amount = 100m,
            Date = fixture.Date,
            PaymentMethod = "credit_card",
            TagIds = []
        };
        var purchase = await fixture.Expenses.CreateAsync(fixture.UserId, purchaseRequest);
        var purchaseRetry = await fixture.Expenses.CreateAsync(fixture.UserId, purchaseRequest);
        Assert.Equal(purchase.Id, purchaseRetry.Id);

        Assert.Null(purchase.AccountId);
        Assert.Equal(500m, await fixture.AccountBalanceAsync());
        Assert.Equal(100m, await fixture.CardBalanceAsync(card.Id));

        var paymentKey = Guid.NewGuid();
        var request = new CreditCardPaymentCreateDto
        {
            SourceAccountId = fixture.AccountId,
            PrincipalAmount = 100m,
            PaymentDate = fixture.Date,
            IdempotencyKey = paymentKey
        };
        var first = await fixture.Cards.AddPaymentAsync(card.Id, fixture.UserId, request);
        var retry = await fixture.Cards.AddPaymentAsync(card.Id, fixture.UserId, request);

        Assert.Equal(first.Id, retry.Id);
        Assert.Equal(400m, await fixture.AccountBalanceAsync());
        Assert.Equal(0m, await fixture.CardBalanceAsync(card.Id));
        Assert.Equal(100m, await fixture.ActiveExpenseTotalAsync());
        Assert.Equal(100m, await fixture.PrincipalPaidAsync());
        Assert.Single(await fixture.Context.CreditCardPayments.ToListAsync());
        Assert.Equal(2, await fixture.Context.CreditCardTransactions.CountAsync(t => t.DeletedAt == null));
    }

    [Fact]
    public async Task VoidPayment_RestoresPrincipalAndCommissionWhileKeepingAuditTrail()
    {
        await using var fixture = await Fixture.CreateAsync();
        var card = await fixture.CreateCardAsync("Corrección");
        await fixture.CreatePurchaseAsync(card.Id, 100m);
        var payment = await fixture.Cards.AddPaymentAsync(card.Id, fixture.UserId, new CreditCardPaymentCreateDto
        {
            SourceAccountId = fixture.AccountId,
            PrincipalAmount = 60m,
            CommissionAmount = 5m,
            CommissionCategoryId = fixture.CategoryId,
            PaymentDate = fixture.Date,
            IdempotencyKey = Guid.NewGuid()
        });
        var voidRequest = new CreditCardPaymentVoidDto
        {
            Date = fixture.Date,
            Reason = "Cuenta origen equivocada",
            IdempotencyKey = Guid.NewGuid()
        };

        var first = await fixture.Cards.VoidPaymentAsync(
            card.Id, payment.Id, fixture.UserId, voidRequest);
        var retry = await fixture.Cards.VoidPaymentAsync(
            card.Id, payment.Id, fixture.UserId, voidRequest);

        Assert.True(first.IsVoided);
        Assert.Equal(first.VoidedAt, retry.VoidedAt);
        Assert.Equal(500m, await fixture.AccountBalanceAsync());
        Assert.Equal(100m, await fixture.CardBalanceAsync(card.Id));
        Assert.Equal(100m, await fixture.ActiveExpenseTotalAsync());
        Assert.Equal(0m, await fixture.PrincipalPaidAsync());
        Assert.Equal(3, await fixture.Context.CreditCardTransactions.CountAsync(t => t.DeletedAt == null));
        Assert.Single(await fixture.Context.CreditCardTransactions.Where(
            t => t.Type == CreditCardTransactionType.PaymentReversal).ToListAsync());
    }

    [Fact]
    public async Task PartialPaymentCommissionAndInterest_KeepPrincipalOutOfExpenses()
    {
        await using var fixture = await Fixture.CreateAsync();
        var card = await fixture.CreateCardAsync("Mastercard");
        await fixture.CreatePurchaseAsync(card.Id, 200m);

        await fixture.Cards.AddPaymentAsync(card.Id, fixture.UserId, new CreditCardPaymentCreateDto
        {
            SourceAccountId = fixture.AccountId,
            PrincipalAmount = 80m,
            CommissionAmount = 5m,
            CommissionCategoryId = fixture.CategoryId,
            PaymentDate = fixture.Date,
            IdempotencyKey = Guid.NewGuid()
        });

        Assert.Equal(415m, await fixture.AccountBalanceAsync());
        Assert.Equal(120m, await fixture.CardBalanceAsync(card.Id));
        Assert.Equal(205m, await fixture.ActiveExpenseTotalAsync());

        await fixture.Cards.AddChargeAsync(card.Id, fixture.UserId, new CreditCardChargeCreateDto
        {
            Type = "interest",
            CategoryId = fixture.CategoryId,
            Amount = 10m,
            Date = fixture.Date,
            IdempotencyKey = Guid.NewGuid()
        });

        Assert.Equal(415m, await fixture.AccountBalanceAsync());
        Assert.Equal(130m, await fixture.CardBalanceAsync(card.Id));
        Assert.Equal(215m, await fixture.ActiveExpenseTotalAsync());
        Assert.Contains(await fixture.Context.CreditCardTransactions.ToListAsync(),
            transaction => transaction.Type == CreditCardTransactionType.Interest && transaction.Amount == 10m);
    }

    [Fact]
    public async Task EditMoveAndDeletePurchase_SynchronizeLiabilityWithoutOrphans()
    {
        await using var fixture = await Fixture.CreateAsync();
        var firstCard = await fixture.CreateCardAsync("Primera");
        var secondCard = await fixture.CreateCardAsync("Segunda");
        var purchase = await fixture.CreatePurchaseAsync(firstCard.Id, 100m);

        await fixture.Expenses.UpdateAsync(purchase.Id, fixture.UserId, new ExpenseUpdateDto
        {
            CategoryId = fixture.CategoryId,
            CreditCardId = secondCard.Id,
            Amount = 120m,
            Date = fixture.Date,
            PaymentMethod = "credit_card",
            TagIds = []
        });

        Assert.Equal(0m, await fixture.CardBalanceAsync(firstCard.Id));
        Assert.Equal(120m, await fixture.CardBalanceAsync(secondCard.Id));
        Assert.Single(await fixture.Context.CreditCardTransactions.Where(t => t.DeletedAt == null).ToListAsync());

        await fixture.Expenses.DeleteAsync(purchase.Id, fixture.UserId);

        Assert.Equal(0m, await fixture.CardBalanceAsync(secondCard.Id));
        Assert.Empty(await fixture.Context.CreditCardTransactions.Where(t => t.DeletedAt == null).ToListAsync());
    }

    [Fact]
    public async Task PaymentRejectsOverpaymentInactiveCardAndForeignOwner()
    {
        await using var fixture = await Fixture.CreateAsync();
        var card = await fixture.CreateCardAsync("Restricciones");
        await fixture.CreatePurchaseAsync(card.Id, 50m);
        var request = new CreditCardPaymentCreateDto
        {
            SourceAccountId = fixture.AccountId,
            PrincipalAmount = 51m,
            PaymentDate = fixture.Date,
            IdempotencyKey = Guid.NewGuid()
        };

        var overpayment = await Assert.ThrowsAsync<DomainException>(() =>
            fixture.Cards.AddPaymentAsync(card.Id, fixture.UserId, request));
        Assert.Equal("CREDIT_CARD_OVERPAYMENT", overpayment.Code);
        Assert.Equal(500m, await fixture.AccountBalanceAsync());
        Assert.Equal(50m, await fixture.CardBalanceAsync(card.Id));

        request.PrincipalAmount = 50m;
        request.CommissionAmount = 500m;
        request.CommissionCategoryId = fixture.CategoryId;
        request.IdempotencyKey = Guid.NewGuid();
        var insufficient = await Assert.ThrowsAsync<DomainException>(() =>
            fixture.Cards.AddPaymentAsync(card.Id, fixture.UserId, request));
        Assert.Equal("INSUFFICIENT_ACCOUNT_BALANCE", insufficient.Code);
        Assert.Equal(500m, await fixture.AccountBalanceAsync());
        Assert.Equal(50m, await fixture.CardBalanceAsync(card.Id));
        Assert.Empty(await fixture.Context.CreditCardPayments.ToListAsync());

        await fixture.Cards.UpdateAsync(card.Id, fixture.UserId, new CreditCardUpdateDto
        {
            Name = card.Name, ClosingDay = card.ClosingDay, DueDay = card.DueDay, IsActive = false
        });
        request.PrincipalAmount = 10m;
        request.IdempotencyKey = Guid.NewGuid();
        var inactive = await Assert.ThrowsAsync<DomainException>(() =>
            fixture.Cards.AddPaymentAsync(card.Id, fixture.UserId, request));
        Assert.Equal("INACTIVE_CREDIT_CARD", inactive.Code);
        Assert.Equal(50m, await fixture.CardLiabilityAsync());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            fixture.Cards.AddPaymentAsync(card.Id, Guid.NewGuid(), request));
    }

    [Fact]
    public async Task ChangingClosingAndDueDays_DoesNotCreateEconomicTransactions()
    {
        await using var fixture = await Fixture.CreateAsync();
        var card = await fixture.CreateCardAsync("Corte");

        await fixture.Cards.UpdateAsync(card.Id, fixture.UserId, new CreditCardUpdateDto
        {
            Name = card.Name,
            ClosingDay = 12,
            DueDay = 28,
            IsActive = true
        });

        Assert.Empty(await fixture.Context.CreditCardTransactions.ToListAsync());
        Assert.Equal(0m, await fixture.ActiveExpenseTotalAsync());
        Assert.Equal(500m, await fixture.AccountBalanceAsync());
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        public AppDbContext Context { get; }
        public CreditCardService Cards { get; }
        public ExpenseService Expenses { get; }
        public Guid UserId { get; }
        public Guid AccountId { get; }
        public Guid CategoryId { get; }
        public DateOnly Date { get; } = DateOnly.FromDateTime(DateTime.Today);

        private Fixture(
            SqliteConnection connection, AppDbContext context,
            CreditCardService cards, ExpenseService expenses,
            Guid userId, Guid accountId, Guid categoryId)
        {
            _connection = connection;
            Context = context;
            Cards = cards;
            Expenses = expenses;
            UserId = userId;
            AccountId = accountId;
            CategoryId = categoryId;
        }

        public static async Task<Fixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            connection.CreateFunction<string>("gen_random_uuid", () => Guid.NewGuid().ToString());
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
            var context = new AppDbContext(options);
            await context.Database.EnsureCreatedAsync();

            var user = new User { Id = Guid.NewGuid(), Email = $"card-{Guid.NewGuid()}@example.test", PasswordHash = "hash", FirstName = "Card", LastName = "Test" };
            var account = new FinancialAccount { Id = Guid.NewGuid(), UserId = user.Id, Name = "Banco", Type = FinancialAccountType.Cash, CurrentBalance = 500m, IsActive = true };
            var category = new Category { Id = Guid.NewGuid(), UserId = user.Id, Name = "Financiero", Type = CategoryType.Expense };
            context.AddRange(user, account, category);
            await context.SaveChangesAsync();

            var unitOfWork = new UnitOfWork(context);
            var accountService = new FinancialAccountService(new FinancialAccountRepository(context), null!, null!, unitOfWork);
            var cardService = new CreditCardService(
                new CreditCardRepository(context), new ExpenseRepository(context),
                new CategoryRepository(context), accountService, unitOfWork);
            var expenseService = new ExpenseService(
                new ExpenseRepository(context), new TagRepository(context),
                accountService, cardService, unitOfWork);
            return new Fixture(connection, context, cardService, expenseService, user.Id, account.Id, category.Id);
        }

        public Task<CreditCardResponseDto> CreateCardAsync(string name) => Cards.CreateAsync(UserId, new CreditCardCreateDto
        {
            Name = name, OpeningBalance = 0m, OpeningDate = Date,
            CreditLimit = 1000m, ClosingDay = 5, DueDay = 20
        });

        public Task<FinanceApp.Application.DTOs.Expense.ExpenseResponseDto> CreatePurchaseAsync(Guid cardId, decimal amount) =>
            Expenses.CreateAsync(UserId, new ExpenseCreateDto
            {
                CategoryId = CategoryId, CreditCardId = cardId,
                IdempotencyKey = Guid.NewGuid(), Amount = amount,
                Date = Date, PaymentMethod = "credit_card", TagIds = []
            });

        public async Task<decimal> AccountBalanceAsync() =>
            (await Context.FinancialAccounts.AsNoTracking().SingleAsync(a => a.Id == AccountId)).CurrentBalance;

        public async Task<decimal> CardBalanceAsync(Guid cardId) =>
            (await Context.CreditCards.AsNoTracking().SingleAsync(c => c.Id == cardId)).CurrentBalance;

        public Task<decimal> ActiveExpenseTotalAsync() => Context.Expenses
            .Where(expense => expense.UserId == UserId && expense.DeletedAt == null)
            .SumAsync(expense => expense.Amount);

        public Task<decimal> PrincipalPaidAsync() =>
            new CreditCardRepository(Context).GetTotalPrincipalPaidByDateRangeAsync(UserId, Date, Date);

        public Task<decimal> CardLiabilityAsync() =>
            new CreditCardRepository(Context).GetTotalCurrentBalanceAsync(UserId);

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
