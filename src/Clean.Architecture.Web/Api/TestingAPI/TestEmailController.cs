using Azure.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;

namespace Clean.Architecture.Web.Api.TestingAPI;

[Route("api/[controller]")]
[ApiController]
public class TestEmailController : ControllerBase
{
  public GraphServiceClient _graphClient;
  public TestEmailController()
  {
   // "Instance": "https://login.microsoftonline.com/",
    //"Domain": "basharo9999hotmail.onmicrosoft.com",
    //"TenantId": "6f64f9eb-73c2-4e0c-b1c6-2bb14c3b2d14",
    //"TenantId": "common",
    //"ClientId": "069395cb-909a-4f2f-8bfc-f4e0265374be",
    //"ClientSecret": "Ujq8Q~mPVcyAiZVoFwjcRIOBj1YRYAYuPepuycdm",
    var scopes = new[] { "https://graph.microsoft.com/.default" };
    var tenantId = "common";
    var clientId = "069395cb-909a-4f2f-8bfc-f4e0265374be";
    var clientSecret = "Ujq8Q~mPVcyAiZVoFwjcRIOBj1YRYAYuPepuycdm";

    // using Azure.Identity;
    //var options = new TokenCredentialOptions
    //{
    //  AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
    //};
    var clientSecretCredential = new ClientSecretCredential(
        tenantId, clientId, clientSecret);

    _graphClient = new GraphServiceClient(clientSecretCredential, scopes);
  }
}
