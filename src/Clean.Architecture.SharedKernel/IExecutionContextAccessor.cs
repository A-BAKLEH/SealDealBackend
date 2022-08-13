namespace Clean.Architecture.SharedKernel;
public interface IExecutionContextAccessor
{
  Guid CorrelationId { get; }

  bool IsAvailable { get; }
}
