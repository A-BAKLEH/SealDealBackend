using Clean.Architecture.Core.BrokerAggregate;
using Clean.Architecture.Core.Interfaces;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;

namespace Clean.Architecture.Infrastructure.Services;
public class MsGraphService : IMsGraphService
{

  public GraphServiceClient _graphClient;
  private readonly IConfigurationSection _MsGraphConfigSection;
  public MsGraphService(IConfiguration config)
  {

    var configSection = config.GetSection("MsGraphOptions");
    var scopes = new[] { "https://graph.microsoft.com/.default" };
    var tenantId = configSection["tenantId"];
    var clientId = configSection["clientId"];
    var clientSecret = configSection["clientSecret"];

    // using Azure.Identity;
    var options = new TokenCredentialOptions
    {
      AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
    };

    var clientSecretCredential = new ClientSecretCredential(
        tenantId, clientId, clientSecret, options);

    _graphClient = new GraphServiceClient(clientSecretCredential, scopes);
  }

  public async Task createB2CUsers(List<Broker> brokers)
  {
    /*var user = new User
    {
      GivenName = brokerDTO.FirstName,
      Surname = brokerDTO.LastName,
      DisplayName = brokerDTO.FirstName + " " + brokerDTO.LastName,
      Identities = new List<ObjectIdentity>
                {
                    new ObjectIdentity()
                    {
                        SignInType = "emailAddress",
                        Issuer = "sealdealtest.onmicrosoft.com",
                        IssuerAssignedId = brokerDTO.Email
                    }
                },
      PasswordProfile = new PasswordProfile()
      {
        Password = "Bashar9!"
      },
      PasswordPolicies = "DisablePasswordExpiration",
    };

    var created = MsGraphClient._graphClient.Users.Request().AddAsync(user).Result;
    */
    // return created.Id;
    throw new NotImplementedException();
  }
}
