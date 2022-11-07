using Microsoft.AspNetCore.Mvc;

namespace Clean.Architecture.SharedKernel.Exceptions.CustomProblemDetails;
public class BadRequestProblemDetails : ProblemDetails
{
  public Object? Errors { get; set; }
}
