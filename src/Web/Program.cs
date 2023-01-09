using Autofac;
using Autofac.Extensions.DependencyInjection;
using Core;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Hangfire;
using Web;
using Web.Config;
using Hellang.Middleware.ProblemDetails;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Exceptions;
using SharedKernel.Exceptions.CustomProblemDetails;
using Web.SignalRInfra;
using Core.Constants;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

var version = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
builder.Host.UseSerilog((_, config) => config.ReadFrom.Configuration(builder.Configuration)
  .WriteTo.Seq("http://localhost:5341", restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information)
  .Enrich.FromLogContext()
  .Enrich.WithProperty("AppVersion", version));

builder.Services.AddHangfire(builder.Configuration.GetConnectionString("DefaultConnection"));

builder.Services.AddControllers();
builder.Services.AddDbContext(builder.Configuration.GetConnectionString("DefaultConnection"));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
  options.AddPolicy(name: MyAllowSpecificOrigins, policy => { policy.WithOrigins("http://localhost:3000").AllowAnyHeader().AllowAnyMethod().AllowCredentials(); });
});
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddMicrosoftIdentityWebApi(options =>
                    {
                      builder.Configuration.Bind("AzureAdB2C", options);
                      options.TokenValidationParameters.NameClaimType = "name";
                    },
                      options =>
                      {
                        builder.Configuration.Bind("AzureAdB2C", options);
                      }
                      );

builder.Services.AddHttpContextAccessor();

builder.Services.AddProblemDetails(options =>
{
  options.IncludeExceptionDetails = (ctx, env) => false;

  options.Map<CustomBadRequestException>(ex => new BadRequestProblemDetails
  {
    Title = ex.title,
    Status = ex.errorCode,
    Detail = ex.details,
    Errors = ex.ErrorsJSON
  });
  options.Map<InconsistentStateException>(ex => new ProblemDetails
  {
    Title = ex.title,
    Status = ex.errorCode,
    Detail = ex.details,
  });

});

if(builder.Environment.IsEnvironment("Test"))
{

}
//add redis in production instead
if (builder.Environment.IsDevelopment())
{
  builder.Services.AddStackExchangeRedisCache(options =>
  {
    //options.Configuration = builder.Configuration.GetConnectionString("redis");
    options.ConfigurationOptions = new StackExchange.Redis.ConfigurationOptions
    {
      EndPoints = { "redis-17282.c56.east-us.azure.cloud.redislabs.com:17282" },
      Password = "m2qOkNVxZXxhXAwncrC5l0vpaCiBj3dc"
    };
    //options.InstanceName = "test1";
  });
  //builder.Services.AddDistributedMemoryCache(option => option.SizeLimit = 26);
  builder.Services.AddSignalR().AddAzureSignalR();
}

builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
  containerBuilder.RegisterModule(new DefaultCoreModule());
  containerBuilder.RegisterModule(new DefaultInfrastructureModule(builder.Environment.EnvironmentName == "Development", builder.Configuration, Assembly.GetExecutingAssembly()));
  containerBuilder.RegisterModule(new WebModule(builder.Environment.EnvironmentName == "Development"));
});
//builder.Services.AddApplicationInsightsTelemetry();

//builder.Logging.AddAzureWebAppDiagnostics(); //add this if deploying to Azure
VariousCons.MainAPIURL = builder.Configuration.GetSection("URLs")["MainAPI"];

var app = builder.Build();

app.UseMiddleware<CorrelationMiddleware>();
app.UseProblemDetails();
if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}
else
{
  //app.UseExceptionHandler();
  //app.UseProblemDetails();
  app.UseHsts();
}

app.UseHttpsRedirection();

app.UseCors(MyAllowSpecificOrigins);
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapHub<NotifsHub>("/notifs");


app.MapControllers();
app.MapHangfireDashboard();
app.Run();

//Add-Migration InitialMigrationName -StartupProject Web -Context AppDbContext -Project Infrastructure
//stripe listen --forward-to https://localhost:7156/api/Webhook/webhook
