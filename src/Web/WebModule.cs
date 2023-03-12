using Autofac;
using Core.ExternalServiceInterfaces.ActionPlans;
using SharedKernel;
using Web.Config;
using Web.ControllerServices;
using Web.ControllerServices.QuickServices;
using Web.Outbox.Config;
using Web.Processing.ActionPlans;
using Web.Processing.EmailAutomation;
using Web.Processing.Various;

namespace Web;

public class WebModule : Module
{

  public WebModule(bool isDevelopment)
  {

  }

  protected override void Load(ContainerBuilder builder)
  {
    builder.RegisterType<ActionExecuter>().As<IActionExecuter>().InstancePerLifetimeScope();



    builder.RegisterType(typeof(AuthorizationService)).AsSelf().InstancePerLifetimeScope();
    builder.RegisterType(typeof(BrokerQService)).AsSelf().InstancePerLifetimeScope();
    builder.RegisterType(typeof(LeadQService)).AsSelf().InstancePerLifetimeScope();
    builder.RegisterType(typeof(AgencyQService)).AsSelf().InstancePerLifetimeScope();
    builder.RegisterType(typeof(ListingQService)).AsSelf().InstancePerLifetimeScope(); 
    builder.RegisterType(typeof(TagQService)).AsSelf().InstancePerLifetimeScope(); 
    builder.RegisterType(typeof(TemplatesQService)).AsSelf().InstancePerLifetimeScope();
    builder.RegisterType(typeof(ActionPQService)).AsSelf().InstancePerLifetimeScope();
    builder.RegisterType(typeof(HandleTodo)).AsSelf().InstancePerLifetimeScope();
    builder.RegisterType(typeof(APProcessor)).AsSelf().InstancePerLifetimeScope();
    
    builder.RegisterType(typeof(ToDoTaskQService)).AsSelf().InstancePerLifetimeScope();

    builder.RegisterType(typeof(EmailProcessor)).AsSelf().InstancePerLifetimeScope();
    
      
    builder.RegisterType(typeof(MSFTEmailQService)).AsSelf().InstancePerLifetimeScope();

    builder.RegisterType(typeof(ExecutionContextAccessor)).As(typeof(IExecutionContextAccessor)).SingleInstance();


    builder.RegisterType(typeof(OutboxDispatcher)).AsSelf().InstancePerLifetimeScope();

  }
}
