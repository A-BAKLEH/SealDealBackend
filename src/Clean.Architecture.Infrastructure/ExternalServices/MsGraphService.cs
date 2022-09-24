
using Azure.Identity;
using Clean.Architecture.Core.Domain.BrokerAggregate;
using Clean.Architecture.Core.ExternalServiceInterfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;

namespace Clean.Architecture.Infrastructure.ExternalServices;
public class MsGraphService : IMsGraphService
{

  public GraphServiceClient _graphClient;
  //private readonly IConfigurationSection _MsGraphConfigSection;
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

  public async Task<string> createB2CUser(Broker broker)
  {
    var user = new User
    {
      GivenName = broker.FirstName,
      Surname = broker.LastName,
      DisplayName = broker.FirstName + " " + broker.LastName,
      Identities = new List<ObjectIdentity>
      {
          new ObjectIdentity()
          {
              SignInType = "emailAddress",
              Issuer = "sealdealtest.onmicrosoft.com",
              IssuerAssignedId = broker.LoginEmail
          }
      },
      PasswordProfile = new PasswordProfile()
      {
        Password = "Bashar9!"
      },
      PasswordPolicies = "DisablePasswordExpiration",
    };

    var created = await _graphClient.Users.Request().AddAsync(user);
    return created.Id;
  }

  public async Task test()
  {
    var scopes = new[] { "https://graph.microsoft.com/.default" };
    var options = new TokenCredentialOptions
    {
      AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
    };
    var clientSecretCredential = new ClientSecretCredential(
        "6f64f9eb-73c2-4e0c-b1c6-2bb14c3b2d14", "f60efc37-251c-4d97-bb07-554000f5057f", "5ud8Q~VO9RxZ9srylubIcMXvuFRWPezGs8gGXaH1", options);

    var  client = new GraphServiceClient(clientSecretCredential, scopes);

    var clients = await client.Users.Request().GetAsync();

    var count = clients.Count;

  }
}
