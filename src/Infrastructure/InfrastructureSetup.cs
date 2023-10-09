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
using Twilio;

namespace Infrastructure;

public static class InfrastructureSetup
{
    public static void AddDbContext(this IServiceCollection services, string connectionString) =>
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString), optionsLifetime: ServiceLifetime.Singleton); // will be created in web project root
    public static void AddDbContextFactory(this IServiceCollection services, string connectionString) =>
        services.AddDbContextFactory<AppDbContext>(
        options =>
            options.UseNpgsql(connectionString));
    public static void AddHangfire(this IServiceCollection services, string connectionString, bool isDev, bool isAdmin, bool isProd)
    {
        services.AddHangfire(configuration => configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
        {
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5), //default
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5), //default
            QueuePollInterval = TimeSpan.Zero, //just for fire and forget jobs - default
            UseRecommendedIsolationLevel = true, //default
            DisableGlobalLocks = true, //not default
            EnableHeavyMigrations = false //not default, put to true when you need to migrate
        }));

        if (isDev)
        {
            services.AddHangfireServer();
        }
        else if(isProd)
        {
            services.AddHangfireServer(options =>
            {
                options.ShutdownTimeout = TimeSpan.FromSeconds(15);
                options.WorkerCount = Math.Min(Environment.ProcessorCount * 3,15);
            });
        }
        else
        {
            //dont add processing in admin mode
        }
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

        var _TwilioSection = config.GetSection("Twilio");
        var accountSid = _TwilioSection["accountSid"];
        var authToken = _TwilioSection["authToken"];
        TwilioClient.Init(accountSid, authToken);

        services.AddMediatR(config => config.RegisterServicesFromAssemblies(_assemblies.ToArray()));
        services.AddScoped<IStripeCheckoutService, StripeCheckoutService>();
        services.AddScoped<IStripeBillingPortalService, StripeBillingPortalService>();
        services.AddScoped<IStripeSubscriptionService, StripeSubscriptionService>();
        services.AddScoped<IB2CGraphService, B2CGraphService>();
        services.AddScoped<ADGraphWrapper>();
    }
}
