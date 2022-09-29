namespace Clean.Architecture.Core.ExternalServiceInterfaces.ProcessingInterfaces;
/// <summary>
/// Provides methods to process Recurren Tasks
/// </summary>
public interface IRecTaskProcessor
{
  Task RunEmailsFetchTask(int TaskId, bool isGmail);
}
