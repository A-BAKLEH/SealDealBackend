using Clean.Architecture.Core.ExternalServiceInterfaces;
using MediatR;

namespace Clean.Architecture.Core.DomainNotifications.BrokerNotifs.Test;
public class BrokerSignedUpNotifHandler : INotificationHandler<BrokerSignedUpNotif>
{
  private readonly IEmailSender _emailSender;
  public BrokerSignedUpNotifHandler(IEmailSender emailSender)
  {
    _emailSender = emailSender;
  }

  public async Task Handle(BrokerSignedUpNotif notification, CancellationToken cancellationToken)
  {
    await _emailSender.SendEmailAsync("s", "s", "BrokerSignedUpNotif Handled", "sss");
  }
}

