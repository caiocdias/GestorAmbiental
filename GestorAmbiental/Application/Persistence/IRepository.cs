using GestorAmbiental.Domain.Common;

namespace GestorAmbiental.Application.Persistence;

public interface IRepository<T> where T : class, IEntity
{
    Task<IReadOnlyList<T>> ListarAsync(CancellationToken cancellationToken = default);

    Task<T?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task<T> SalvarAsync(T entity, CancellationToken cancellationToken = default);

    Task ExcluirAsync(int id, CancellationToken cancellationToken = default);
}
