using Autofac;
using Autofac.Extensions.DependencyInjection;
using Clean.Architecture.Core;
using Clean.Architecture.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Hangfire;
using Clean.Architecture.Web;
using Clean.Architecture.Web.Config;
using Hellang.Middleware.ProblemDetails;
using Clean.Architecture.Web.Config.ProblemDetails;
using Clean.Architecture.SharedKernel.BusinessRules;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

var version = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
builder.Host.UseSerilog((_, config) => config.ReadFrom.Configuration(builder.Configuration)
  .WriteTo.Seq("http://localhost:5341", restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information)
  .Enrich.FromLogContext()
  .Enrich.WithProperty("AppVersion",version));

builder.Services.AddHangfire(builder.Configuration.GetConnectionString("DefaultConnection"));
/*builder.Services.Configure<CookiePolicyOptions>(options =>
{
  options.CheckConsentNeeded = context => true;
  options.MinimumSameSitePolicy = SameSiteMode.None;
});*/
builder.Services.AddControllers();
builder.Services.AddDbContext(builder.Configuration.GetConnectionString("DefaultConnection"));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
  options.AddPolicy(name: MyAllowSpecificOrigins,policy => { policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();});
});
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddMicrosoftIdentityWebApi(options =>
                    {
                      builder.Configuration.Bind("AzureAdB2C", options);
                      options.TokenValidationParameters.NameClaimType = "name";
                    },
                    options => { builder.Configuration.Bind("AzureAdB2C", options); });

builder.Services.AddHttpContextAccessor();

builder.Services.AddProblemDetails(x =>
{
  //x.Map<InvalidCommandException>(ex => new InvalidCommandProblemDetails(ex));
  x.Map<BusinessRuleValidationException>(ex => new BusinessRuleValidationExceptionProblemDetails(ex));
});

/*builder.Services.AddProblemDetails(options =>
{
  // Only include exception details in a development environment. There's really no need
  // to set this as it's the default behavior. It's just included here for completeness :)
  options.IncludeExceptionDetails = (ctx, ex) => builder.Environment.IsDevelopment();

  // This will map UserNotFoundException to the 404 Not Found status code and return custom problem details.
  options.Map<UserNotFoundException>(ex => new ProblemDetails
  {
    Title = "Could not find user",
    Status = StatusCodes.Status404NotFound,
    Detail = ex.Message,
  });

  // This will map NotImplementedException to the 501 Not Implemented status code.
  options.MapToStatusCode<NotImplementedException>(StatusCodes.Status501NotImplemented);

  // You can configure the middleware to re-throw certain types of exceptions, all exceptions or based on a predicate.
  // This is useful if you have upstream middleware that  needs to do additional handling of exceptions.
  options.Rethrow<NotSupportedException>();

  // You can configure the middleware to ingore any exceptions of the specified type.
  // This is useful if you have upstream middleware that  needs to do additional handling of exceptions.
  // Note that unlike Rethrow, additional information will not be added to the exception.
  options.Ignore<DivideByZeroException>();

  // Because exceptions are handled polymorphically, this will act as a "catch all" mapping, which is why it's added last.
  // If an exception other than NotImplementedException and HttpRequestException is thrown, this will handle it.
  options.MapToStatusCode<Exception>(StatusCodes.Status500InternalServerError);
});*/



builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
  containerBuilder.RegisterModule(new DefaultCoreModule());
  containerBuilder.RegisterModule(new DefaultInfrastructureModule(builder.Environment.EnvironmentName == "Development"));
  containerBuilder.RegisterModule(new WebModule(builder.Environment.EnvironmentName == "Development"));
});
//builder.Services.AddApplicationInsightsTelemetry();

//builder.Logging.AddAzureWebAppDiagnostics(); //add this if deploying to Azure

var app = builder.Build();

app.UseMiddleware<CorrelationMiddleware>();

if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}
else
{
  //app.UseExceptionHandler();
  app.UseProblemDetails();
  app.UseHsts();
}
//app.UseRouting();

app.UseHttpsRedirection();
//app.UseStaticFiles();
//app.UseCookiePolicy();


app.UseAuthentication();
app.UseAuthorization();
app.UseCors(MyAllowSpecificOrigins);

app.MapControllers();
app.MapHangfireDashboard();
app.Run();

//Add-Migration InitialMigrationName -StartupProject Clean.Architecture.Web -Context AppDbContext -Project Clean.Architecture.Infrastructure
//stripe listen --forward-to https://localhost:7156/api/Webhook/webhook
