using Autofac;
using Clean.Architecture.SharedKernel;
using Clean.Architecture.Web.Config;
using Clean.Architecture.Web.ControllerServices;
namespace Clean.Architecture.Web;

public class WebModule : Module
{

  public WebModule(bool isDevelopment)
  {

  }

  protected override void Load(ContainerBuilder builder)
  {
    builder.RegisterType(typeof(AuthorizationService)).AsSelf().SingleInstance();

    builder.RegisterType(typeof(ExecutionContextAccessor)).As(typeof(IExecutionContextAccessor)).SingleInstance();
  }
}
