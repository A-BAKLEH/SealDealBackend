

namespace Clean.Architecture.SharedKernel.Exceptions;
/// <summary>
/// Error in backend not caused by this specific request handling, needs explicit fixing on the backend.
/// Will Return 500 error response code
/// </summary>
public class InconsistentStateException : Exception
{
  public string details { get; private set; }
  public string title { get; private set; }
  public int errorCode { get; private set; }
  public string tag { get; private set; }
  public string? UserId { get; private set; }

  public InconsistentStateException(string tag,string details, string title,int errorCode = 500, string? UserId = null ) : base(tag + ": " +details)
  {
    this.details = details;
    this.title = title;
    this.errorCode = errorCode;
    this.tag = tag;
    this.UserId = UserId;
  }
}
