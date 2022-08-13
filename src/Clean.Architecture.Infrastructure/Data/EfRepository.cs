using Ardalis.Specification.EntityFrameworkCore;
using Clean.Architecture.SharedKernel.Interfaces;

namespace Clean.Architecture.Infrastructure.Data;

// inherit from Ardalis.Specification type
public class EfRepository<T> : RepositoryBase<T>, IReadRepository<T>, IRepository<T> where T : class, IAggregateRoot
{
  private readonly AppDbContext _appDbContext;
  public EfRepository(AppDbContext dbContext) : base(dbContext)
  {
    _appDbContext = dbContext;
  }
}
