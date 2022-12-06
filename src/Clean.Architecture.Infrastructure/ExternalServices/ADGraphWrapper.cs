

using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;

namespace Clean.Architecture.Infrastructure.ExternalServices;
public class ADGraphWrapper
{
  private readonly IConfigurationSection _configurationSection;
  public GraphServiceClient _graphClient;
  public ADGraphWrapper(IConfiguration config)
  {
    _configurationSection = config.GetSection("AzureADGraphOptions");
  }

  public GraphServiceClient CreateClient(string tenantId)
  {
    var scopes = new[] { "https://graph.microsoft.com/.default" };
    var clientId = _configurationSection["ClientId"];
    var clientSecret = _configurationSection["ClientSecret"];

    // using Azure.Identity;
    var options = new TokenCredentialOptions
    {
      AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
    };
    var clientSecretCredential = new ClientSecretCredential(
        tenantId, clientId, clientSecret, options);

    _graphClient = new GraphServiceClient(clientSecretCredential, scopes);
    return _graphClient;
  }

  public async Task<bool> testEmailConnected(string email)
  {
    var messsages = await _graphClient.Users[email].Messages.Request().GetAsync();
    return messsages != null;
  }
}
