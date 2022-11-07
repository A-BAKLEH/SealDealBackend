namespace Clean.Architecture.SharedKernel.Exceptions;

public class CustomBadRequestException : Exception
{
  public string details { get; private set; }
  public string title { get; private set; }
  public int errorCode { get; private set; }
  public Object? ErrorsJSON { get; set; }

  public CustomBadRequestException(string details, string title, int errorCode = 400) : base(title + " : " + details)
  {
    this.details = details;
    this.title = title;
    this.errorCode = errorCode;
  }
}
  
