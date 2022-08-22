namespace Clean.Architecture.SharedKernel.BusinessRules;
public interface IBusinessRule
{
  bool IsBroken();

  string Message { get; }
}
