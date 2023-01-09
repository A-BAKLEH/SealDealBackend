using Microsoft.AspNetCore.Mvc;

namespace SharedKernel.Exceptions.CustomProblemDetails;
public class BadRequestProblemDetails : ProblemDetails
{
  public Object? Errors { get; set; }
}
