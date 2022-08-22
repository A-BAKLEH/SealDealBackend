

namespace Clean.Architecture.SharedKernel.Exceptions;
public class InconsistentStateException : Exception
{
  public string details { get; private set; }
  public string tag { get; private set; }
  public string? UserId { get; private set; }

  public InconsistentStateException(string tag, string message, string? UserId = null ) : base(tag + ": " +message)
  {
    this.details = message;
    this.tag = tag;
    this.UserId = UserId;
  }
}
