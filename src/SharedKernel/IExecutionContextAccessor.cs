namespace SharedKernel;
public interface IExecutionContextAccessor
{
  Guid CorrelationId { get; }

  bool IsAvailable { get; }
}
