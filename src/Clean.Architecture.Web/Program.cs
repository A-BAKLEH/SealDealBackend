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
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Clean.Architecture.SharedKernel.Exceptions;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

var version = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
builder.Host.UseSerilog((_, config) => config.ReadFrom.Configuration(builder.Configuration)
  .WriteTo.Seq("http://localhost:5341", restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information)
  .Enrich.FromLogContext()
  .Enrich.WithProperty("AppVersion", version));

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
  options.AddPolicy(name: MyAllowSpecificOrigins, policy => { policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod(); });
});
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddMicrosoftIdentityWebApi(options =>
                    {
                      builder.Configuration.Bind("AzureAdB2C", options);
                      options.TokenValidationParameters.NameClaimType = "name";
                    },
                    options => { builder.Configuration.Bind("AzureAdB2C", options); });

builder.Services.AddHttpContextAccessor();

builder.Services.AddProblemDetails(options =>
{
  options.Map<CustomBadRequestException>(ex => new ProblemDetails
  {
    Title = "Custom Bad Request",
    Status = StatusCodes.Status400BadRequest,
    Detail = ex.Message,
  });
});

//add redis in production instead
if (builder.Environment.IsDevelopment())
{
  builder.Services.AddDistributedMemoryCache(option => option.SizeLimit = 26);
  builder.Services.AddSignalR().AddAzureSignalR();
}

builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
  containerBuilder.RegisterModule(new DefaultCoreModule());
  containerBuilder.RegisterModule(new DefaultInfrastructureModule(builder.Environment.EnvironmentName == "Development",builder.Configuration,Assembly.GetExecutingAssembly()));
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

app.UseHttpsRedirection();
//app.UseStaticFiles();
//app.UseCookiePolicy();

app.UseCors(MyAllowSpecificOrigins);
app.UseAuthentication();
app.UseAuthorization();


app.MapControllers();
app.MapHangfireDashboard();
app.Run();

//Add-Migration InitialMigrationName -StartupProject Clean.Architecture.Web -Context AppDbContext -Project Clean.Architecture.Infrastructure
//stripe listen --forward-to https://localhost:7156/api/Webhook/webhook
