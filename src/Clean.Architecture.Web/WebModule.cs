using Autofac;
using Clean.Architecture.SharedKernel;
using Clean.Architecture.Web.Config;
using Clean.Architecture.Web.ControllerServices;
using Clean.Architecture.Web.ControllerServices.QuickServices;
using Clean.Architecture.Web.Processing.EmailAutomation;

namespace Clean.Architecture.Web;

public class WebModule : Module
{

  public WebModule(bool isDevelopment)
  {

  }

  protected override void Load(ContainerBuilder builder)
  {
    builder.RegisterType(typeof(AuthorizationService)).AsSelf().InstancePerLifetimeScope();
    builder.RegisterType(typeof(BrokerQService)).AsSelf().InstancePerLifetimeScope();
    builder.RegisterType(typeof(LeadQService)).AsSelf().InstancePerLifetimeScope();
    builder.RegisterType(typeof(AgencyQService)).AsSelf().InstancePerLifetimeScope();
    builder.RegisterType(typeof(ListingQService)).AsSelf().InstancePerLifetimeScope(); 
    builder.RegisterType(typeof(TagQService)).AsSelf().InstancePerLifetimeScope(); 
    builder.RegisterType(typeof(TemplatesQService)).AsSelf().InstancePerLifetimeScope();
    builder.RegisterType(typeof(SubscriptionService)).AsSelf().InstancePerLifetimeScope();

    builder.RegisterType(typeof(MSFTWebhookHandler)).AsSelf().InstancePerLifetimeScope();
    builder.RegisterType(typeof(EmailProcessor)).AsSelf().InstancePerLifetimeScope();
    
      
    builder.RegisterType(typeof(MSFTEmailQService)).AsSelf().InstancePerLifetimeScope();

    builder.RegisterType(typeof(ExecutionContextAccessor)).As(typeof(IExecutionContextAccessor)).SingleInstance();

  }
}
