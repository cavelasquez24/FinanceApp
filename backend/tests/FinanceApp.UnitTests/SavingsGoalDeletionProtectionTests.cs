using FinanceApp.Application.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.Interfaces.Repositories;

namespace FinanceApp.UnitTests;

public class SavingsGoalDeletionProtectionTests
{
    [Fact]
    public async Task Delete_WithPositiveBalance_IsRejectedAndDoesNotArchiveGoal()
    {
        var userId = Guid.NewGuid();
        var goal = new SavingsGoal { Id = Guid.NewGuid(), UserId = userId, Name = "Reserva", CurrentAmount = 1m };
        var repository = new FakeSavingsGoalRepository(goal);
        var service = new SavingsGoalService(repository, null!);

        var error = await Assert.ThrowsAsync<DomainException>(() => service.DeleteAsync(goal.Id, userId));

        Assert.Equal("GOAL_ARCHIVE_RESOLUTION_REQUIRED", error.Code);
        Assert.Null(goal.DeletedAt);
        Assert.False(repository.Updated);
    }

    private sealed class FakeSavingsGoalRepository(SavingsGoal goal) : ISavingsGoalRepository
    {
        public bool Updated { get; private set; }
        public Task<SavingsGoal?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<SavingsGoal?>(goal.Id == id ? goal : null);
        public Task<IEnumerable<SavingsGoal>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<SavingsGoal>>([goal]);
        public Task<SavingsGoal> CreateAsync(SavingsGoal entity, CancellationToken cancellationToken = default) => Task.FromResult(entity);
        public Task<SavingsGoal> UpdateAsync(SavingsGoal entity, CancellationToken cancellationToken = default) { Updated = true; return Task.FromResult(entity); }
        public Task DeleteAsync(SavingsGoal entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IEnumerable<SavingsGoal>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<SavingsGoal>>([goal]);
        public Task<decimal> GetTotalSavedAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult(goal.CurrentAmount);
        public Task<SavingsGoal?> GetByIdWithHistoryAsync(Guid id, Guid userId, CancellationToken cancellationToken = default) => Task.FromResult<SavingsGoal?>(goal.Id == id && goal.UserId == userId ? goal : null);
        public Task AddContributionAsync(SavingsGoalContribution contribution, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddWithdrawalAsync(SavingsGoalWithdrawal withdrawal, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<decimal> GetTotalContributionsByDateRangeAsync(Guid userId, DateOnly start, DateOnly end, CancellationToken cancellationToken = default) => Task.FromResult(0m);
        public Task<decimal> GetTotalCashFlowWithdrawalsByDateRangeAsync(Guid userId, DateOnly start, DateOnly end, CancellationToken cancellationToken = default) => Task.FromResult(0m);
        public Task<decimal> GetTotalConsumedWithdrawalsByDateRangeAsync(Guid userId, DateOnly start, DateOnly end, CancellationToken cancellationToken = default) => Task.FromResult(0m);
    }
}
