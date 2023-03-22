using Autofac;
using Core.Domain.AgencyAggregate;
using Core.ExternalServiceInterfaces;
using Core.ExternalServiceInterfaces.StripeInterfaces;
using Infrastructure.ExternalServices;
using Infrastructure.ExternalServices.Stripe;
using MediatR;
using MediatR.Pipeline;
using Microsoft.Extensions.Configuration;
using Stripe;
using System.Reflection;
using Module = Autofac.Module;

namespace Infrastructure;

public class DefaultInfrastructureModule : Module
{
    private readonly bool _isDevelopment = false;
    private readonly List<Assembly> _assemblies = new List<Assembly>();
    private readonly IConfiguration _config;

    public DefaultInfrastructureModule(bool isDevelopment, IConfiguration config, Assembly? callingAssembly = null)
    {
        _config = config;
        _isDevelopment = isDevelopment;
        var coreAssembly =
          Assembly.GetAssembly(typeof(Agency)); // TODO: Replace "Project" with any type from your Core project
        var infrastructureAssembly = Assembly.GetAssembly(typeof(StartupSetup));
        if (coreAssembly != null)
        {
            _assemblies.Add(coreAssembly);
        }

        if (infrastructureAssembly != null)
        {
            _assemblies.Add(infrastructureAssembly);
        }

        if (callingAssembly != null)
        {
            _assemblies.Add(callingAssembly);
        }
    }

    protected override void Load(ContainerBuilder builder)
    {
        if (_isDevelopment)
        {
            RegisterDevelopmentOnlyDependencies(builder);
        }
        else
        {
            RegisterProductionOnlyDependencies(builder);
        }

        RegisterCommonDependencies(builder);
    }

    private void RegisterCommonDependencies(ContainerBuilder builder)
    {
        var _stripeConfigSection = _config.GetSection("StripeOptions");
        StripeConfiguration.ApiKey = _stripeConfigSection["APIKey"];

        builder
          .RegisterType<Mediator>()
          .As<IMediator>()
          .InstancePerLifetimeScope();

        builder.Register<ServiceFactory>(context =>
        {
            var c = context.Resolve<IComponentContext>();

            return t => c.Resolve(t);
        });

        var mediatrOpenTypes = new[]
        {
      typeof(IRequestHandler<,>),
      typeof(IRequestExceptionHandler<,,>),
      typeof(IRequestExceptionAction<,>),
      typeof(INotificationHandler<>),
    };

        foreach (var mediatrOpenType in mediatrOpenTypes)
        {
            builder
              .RegisterAssemblyTypes(_assemblies.ToArray())
              .AsClosedTypesOf(mediatrOpenType)
              .AsImplementedInterfaces();
        }

        builder.RegisterType<EmailSender>().As<IEmailSender>()
        .InstancePerLifetimeScope();


        builder.RegisterType<StripeCheckoutService>()
        .As<IStripeCheckoutService>()
        .InstancePerLifetimeScope();

        builder.RegisterType<StripeBillingPortalService>()
        .As<IStripeBillingPortalService>()
        .InstancePerLifetimeScope();

        builder.RegisterType<StripeSubscriptionService>()
        .As<IStripeSubscriptionService>()
        .InstancePerLifetimeScope();

        builder.RegisterType<B2CGraphService>()
        .As<IB2CGraphService>()
        .InstancePerLifetimeScope();

        builder.RegisterType<ADGraphWrapper>()
          .AsSelf()
          .InstancePerLifetimeScope();
    }

    private void RegisterDevelopmentOnlyDependencies(ContainerBuilder builder)
    {
        // NOTE: Add any development only services here
    }

    private void RegisterProductionOnlyDependencies(ContainerBuilder builder)
    {
        // NOTE: Add any production only services here
    }
}
