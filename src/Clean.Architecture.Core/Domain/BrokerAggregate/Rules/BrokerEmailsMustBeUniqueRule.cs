
using Clean.Architecture.SharedKernel.BusinessRules;

namespace Clean.Architecture.Core.Domain.BrokerAggregate.Rules;
public class BrokerEmailsMustBeUniqueRule : IBusinessRule
{

  private readonly string _email;

  public BrokerEmailsMustBeUniqueRule(
      string email)
  {
    _email = email;
  }

  //actually implement this
  public bool IsBroken() => false;

  public string Message => "Broker with this email already exists.";
}
