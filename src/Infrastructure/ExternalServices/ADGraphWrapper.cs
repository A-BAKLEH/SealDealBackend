﻿using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;

namespace Infrastructure.ExternalServices;
/// <summary>
/// call CreateClient only once before using
/// </summary>
public class ADGraphWrapper
{
    private readonly IConfigurationSection _configurationSection;
    public GraphServiceClient? _graphClient;
    public ADGraphWrapper(IConfiguration config)
    {
        _configurationSection = config.GetSection("AzureADGraphOptions");
    }

    public GraphServiceClient CreateClient(string tenantId)
    {
        if (_graphClient == null)
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
        }
        return _graphClient;
    }

    /// <summary>
    /// always creates and returns new client. does not save it in adGraphWrapper
    /// </summary>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    public GraphServiceClient CreateExtraClient(string tenantId)
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

        return new GraphServiceClient(clientSecretCredential, scopes);
    }
}
