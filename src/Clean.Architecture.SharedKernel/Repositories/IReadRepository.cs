using Ardalis.Specification;

namespace Clean.Architecture.SharedKernel.Repositories;

public interface IReadRepository<T> : IReadRepositoryBase<T> where T : class, IAggregateRoot
{
}
