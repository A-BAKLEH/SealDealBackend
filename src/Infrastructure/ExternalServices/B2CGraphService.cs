
using Azure.Identity;
using Core.Domain.BrokerAggregate;
using Core.ExternalServiceInterfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Infrastructure.ExternalServices;
public class B2CGraphService : IB2CGraphService
{

    public GraphServiceClient _graphClient;
    //private readonly IConfigurationSection _MsGraphConfigSection;
    public B2CGraphService(IConfiguration config)
    {

        var configSection = config.GetSection("B2CGraphOptions");
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

    public async Task DeleteB2CUserAsync(Guid brokerId)
    {
        try
        {
            await _graphClient.Users[$"{brokerId}"].DeleteAsync();
        }
        catch (Exception ex)
        {
            await Task.Delay(1000);
            await _graphClient.Users[$"{brokerId}"].DeleteAsync();
        }
    }
    public async Task<Tuple<string, string>> createB2CUser(Broker broker)
    {
        //TODO make password random
        var password = "Bashar9!";

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
                Password = password
            },
            PasswordPolicies = "DisablePasswordExpiration",
        };

        var created = await _graphClient.Users.PostAsync(user);
        return Tuple.Create(created.Id, password);
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

        var client = new GraphServiceClient(clientSecretCredential, scopes);

        var clients = await client.Users.GetAsync();
    }
}
