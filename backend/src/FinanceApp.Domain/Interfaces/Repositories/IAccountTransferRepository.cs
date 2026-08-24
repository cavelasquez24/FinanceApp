using FinanceApp.Domain.Entities;

namespace FinanceApp.Domain.Interfaces.Repositories;

public interface IAccountTransferRepository : IBaseRepository<AccountTransfer>
{
    /// <summary>
    /// Trae una AccountTransfer con FromAccount y ToAccount cargados,
    /// validando ownership por UserId.
    /// </summary>
    Task<AccountTransfer?> GetOwnedByIdAsync(
        Guid id, Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AccountTransfer>> GetByUserIdAsync(
        Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca por TransferGroupId para el chequeo de idempotencia en creación
    /// y para la futura consulta de grupo (transferencia + reversa).
    /// </summary>
    Task<AccountTransfer?> GetByTransferGroupIdAsync(
        Guid userId, Guid transferGroupId, CancellationToken cancellationToken = default);
}
