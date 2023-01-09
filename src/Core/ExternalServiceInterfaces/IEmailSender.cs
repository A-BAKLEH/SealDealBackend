using System.Threading.Tasks;

namespace Core.ExternalServiceInterfaces;

public interface IEmailSender
{
  Task SendEmailAsync(string to, string from, string subject, string body);
}
