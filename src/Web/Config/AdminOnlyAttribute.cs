using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Web.Config;

public class AdminOnlyAttribute : Attribute, IFilterFactory
{
    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        return new AdminOnlyAttributeImpl(serviceProvider.GetRequiredService<IWebHostEnvironment>());
    }

    public bool IsReusable => true;

    private class AdminOnlyAttributeImpl : Attribute, IAuthorizationFilter
    {
        public AdminOnlyAttributeImpl(IWebHostEnvironment hostingEnv)
        {
            HostingEnv = hostingEnv;
        }

        private IWebHostEnvironment HostingEnv { get; }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (HostingEnv.EnvironmentName != "Admin")
            {
                context.Result = new NotFoundResult();
            }
        }
    }
}
