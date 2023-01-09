
namespace Core.Domain.NotificationAggregate.HelperObjects;
public class AbstractSmsData
{
  public DateTime SmsTimeStamp { get; set; }

  //any complementary info, maybe to facilitate retreival later on

  public string TextMessage { get; set; } = "";
}
