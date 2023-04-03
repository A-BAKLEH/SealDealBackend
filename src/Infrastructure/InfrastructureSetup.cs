using Core.Domain.AgencyAggregate;
using Core.ExternalServiceInterfaces;
using Core.ExternalServiceInterfaces.StripeInterfaces;
using Hangfire;
using Hangfire.SqlServer;
using Infrastructure.Data;
using Infrastructure.ExternalServices;
using Infrastructure.ExternalServices.Stripe;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Stripe;
using System.Reflection;

namespace Infrastructure;

public static class InfrastructureSetup
{
    public static void AddDbContext(this IServiceCollection services, string connectionString) =>
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString)); // will be created in web project root

    public static void AddHangfire(this IServiceCollection services, string connectionString)
    {
        services.AddHangfire(configuration => configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
        {
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.Zero, //just for fire and forget jobs
            UseRecommendedIsolationLevel = true,
            DisableGlobalLocks = true
        }));

        services.AddHangfireServer();
    }


    public static void AddInfrastructureServices(this IServiceCollection services, IConfiguration config, Assembly webAssembly)
    {
        List<Assembly> _assemblies = new List<Assembly>();

        var coreAssembly = Assembly.GetAssembly(typeof(Agency)); // TODO: Replace "Project" with any type from your Core project
        var infrastructureAssembly = Assembly.GetAssembly(typeof(InfrastructureSetup));
        _assemblies.Add(coreAssembly);
        _assemblies.Add(infrastructureAssembly);
        _assemblies.Add(webAssembly);

        var _stripeConfigSection = config.GetSection("StripeOptions");
        StripeConfiguration.ApiKey = _stripeConfigSection["APIKey"];


        services.AddMediatR(config => config.RegisterServicesFromAssemblies(_assemblies.ToArray()));


        services.AddScoped<IEmailSender, EmailSender>();
        services.AddScoped<IStripeCheckoutService, StripeCheckoutService>();
        services.AddScoped<IStripeBillingPortalService, StripeBillingPortalService>();
        services.AddScoped<IStripeSubscriptionService, StripeSubscriptionService>();
        services.AddScoped<IB2CGraphService, B2CGraphService>();
        services.AddScoped<ADGraphWrapper>();
    }
}
