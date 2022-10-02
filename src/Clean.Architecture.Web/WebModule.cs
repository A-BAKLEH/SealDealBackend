using Autofac;
using Clean.Architecture.Core.ExternalServiceInterfaces.ProcessingInterfaces;
using Clean.Architecture.SharedKernel;
using Clean.Architecture.Web.Config;
using Clean.Architecture.Web.ControllerServices;
using Clean.Architecture.Web.InterfaceImplementations;

namespace Clean.Architecture.Web;

public class WebModule : Module
{

  public WebModule(bool isDevelopment)
  {

  }

  protected override void Load(ContainerBuilder builder)
  {
    builder.RegisterType(typeof(AuthorizationService)).AsSelf().InstancePerLifetimeScope();

    builder.RegisterType(typeof(ExecutionContextAccessor)).As(typeof(IExecutionContextAccessor)).SingleInstance();
    builder.RegisterType(typeof(RecTaskProcessor)).As(typeof(IRecTaskProcessor)).InstancePerLifetimeScope();
  }
}
