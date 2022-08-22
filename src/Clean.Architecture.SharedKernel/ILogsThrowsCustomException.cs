namespace Clean.Architecture.SharedKernel;
public interface ILogsThrowsCustomException
{
  void RaiseAndLogInconsistentStateException(string tag, string details, string? UserId);
}
