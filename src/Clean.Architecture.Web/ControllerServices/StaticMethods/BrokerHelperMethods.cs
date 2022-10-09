using Clean.Architecture.Web.ApiModels.RequestDTOs;

namespace Clean.Architecture.Web.ControllerServices.StaticMethods;

public static class BrokerHelperMethods
{
  public static bool BrokerDTOValid(NewBrokerDTO brokerDTO)
  {
    bool valid = true;
    if (string.IsNullOrWhiteSpace(brokerDTO.FirstName)) { brokerDTO.failureReason += "first name invalid;"; valid = false; }
    if (string.IsNullOrWhiteSpace(brokerDTO.LastName)) { brokerDTO.failureReason += "first name invalid;"; valid = false; }
    if (!IsValidEmail(brokerDTO.Email)) { brokerDTO.failureReason += "email invalid;"; valid = false; }
    return valid;
  }

  public static bool IsValidEmail(string email)
  {
    try
    {
      var addr = new System.Net.Mail.MailAddress(email);
      return addr.Address == email && email.Contains('.');
    }
    catch
    {
      return false;
    }
  }
}
