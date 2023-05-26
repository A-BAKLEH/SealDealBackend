
namespace Core.DTOs.ProcessingDTOs;
public class BrokerForListDTO
{
  public Guid Id { get; set; }
  public string FirstName { get; set; }
  public string LastName { get; set; }
  public Boolean AccountActive { get; set; }
  public string SigninEmail { get; set; }
  public string PhoneNumber { get; set; }
  public DateTime created { get; set; }
}
