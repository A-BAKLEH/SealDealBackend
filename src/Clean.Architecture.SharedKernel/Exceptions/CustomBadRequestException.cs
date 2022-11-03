namespace Clean.Architecture.SharedKernel.Exceptions;
public class CustomBadRequestException : Exception
{
  public string message { get; private set; }
  public CustomBadRequestException(string message): base(message)
  {
    this.message = message;
  }
}
