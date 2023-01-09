namespace SharedKernel;
public abstract class Entity<TId> : EntityBase
{
  public TId Id { get; set; }
}
