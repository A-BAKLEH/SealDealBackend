using Clean.Architecture.SharedKernel.BusinessRules;

namespace Clean.Architecture.Web.Config.ProblemDetails;

public class BusinessRuleValidationExceptionProblemDetails : Microsoft.AspNetCore.Mvc.ProblemDetails
{
  public BusinessRuleValidationExceptionProblemDetails(BusinessRuleValidationException exception)
  {
    this.Title = "Business rule validation error";
    this.Status = StatusCodes.Status409Conflict;
    this.Detail = exception.Details;
  }
}
