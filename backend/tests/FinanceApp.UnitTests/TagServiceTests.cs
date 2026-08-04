using FinanceApp.Application.DTOs.Tag;
using FinanceApp.Application.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Exceptions;
using FinanceApp.Domain.Interfaces.Repositories;
using FinanceApp.Domain.Models;

namespace FinanceApp.UnitTests;

public class TagServiceTests
{
    [Fact]
    public async Task Create_NormalizesNameAndAssignsOwner()
    {
        var repository = new FakeTagRepository();
        var service = new TagService(repository);
        var userId = Guid.NewGuid();

        var result = await service.CreateAsync(userId, new TagCreateDto { Name = "  Con   Amigos  ", Color = "#5c7a99" });

        Assert.Equal("Con Amigos", result.Name);
        Assert.Equal("#5C7A99", result.Color);
        Assert.Equal(userId, repository.Items.Single().UserId);
        Assert.Equal("con amigos", repository.Items.Single().NormalizedName);
    }

    [Fact]
    public async Task Create_RejectsDuplicateActiveNameIgnoringCaseAndSpaces()
    {
        var userId = Guid.NewGuid();
        var repository = new FakeTagRepository();
        repository.Items.Add(new Tag { Id = Guid.NewGuid(), UserId = userId, Name = "Amigos", NormalizedName = "amigos" });
        var service = new TagService(repository);

        await Assert.ThrowsAsync<DomainException>(() =>
            service.CreateAsync(userId, new TagCreateDto { Name = " AMIGOS " }));
    }

    [Fact]
    public async Task Merge_RejectsSelfMerge()
    {
        var service = new TagService(new FakeTagRepository());
        var id = Guid.NewGuid();

        await Assert.ThrowsAsync<DomainException>(() =>
            service.MergeAsync(id, Guid.NewGuid(), new TagMergeDto { TargetTagId = id }));
    }

    private sealed class FakeTagRepository : ITagRepository
    {
        public List<Tag> Items { get; } = new();

        public Task<Tag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.SingleOrDefault(t => t.Id == id));
        public Task<IEnumerable<Tag>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<Tag>>(Items);
        public Task<Tag> CreateAsync(Tag entity, CancellationToken cancellationToken = default)
        {
            entity.Id = entity.Id == Guid.Empty ? Guid.NewGuid() : entity.Id;
            Items.Add(entity);
            return Task.FromResult(entity);
        }
        public Task<Tag> UpdateAsync(Tag entity, CancellationToken cancellationToken = default) => Task.FromResult(entity);
        public Task DeleteAsync(Tag entity, CancellationToken cancellationToken = default)
        {
            Items.Remove(entity);
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<Tag>> GetByUserAsync(Guid userId, string? search = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Tag>>(Items.Where(t => t.UserId == userId && !t.IsDeleted).ToList());
        public Task<IReadOnlyList<Tag>> GetActiveByIdsAsync(Guid userId, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Tag>>(Items.Where(t => t.UserId == userId && ids.Contains(t.Id) && !t.IsDeleted).ToList());
        public Task<int> CountActiveAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Count(t => t.UserId == userId && !t.IsDeleted));
        public Task<bool> ExistsActiveAsync(Guid userId, string normalizedName, Guid? excludingId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Any(t => t.UserId == userId && t.NormalizedName == normalizedName && !t.IsDeleted && t.Id != excludingId));
        public Task MergeAsync(Tag source, Tag target, CancellationToken cancellationToken = default)
        {
            source.DeletedAt = DateTimeOffset.UtcNow;
            source.MergedIntoTagId = target.Id;
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<TagExpenseMetric>> GetExpenseReportAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TagExpenseMetric>>(Array.Empty<TagExpenseMetric>());
        public Task<(int TotalExpenses, int TaggedExpenses)> GetCoverageAsync(Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default) =>
            Task.FromResult((0, 0));
    }
}
