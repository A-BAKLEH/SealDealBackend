using System.Threading.Tasks;

namespace Clean.Architecture.Core.ServiceInterfaces;

public interface IEmailSender
{
  Task SendEmailAsync(string to, string from, string subject, string body);
}
