using Core.Constants;
using Hangfire;
using Hellang.Middleware.ProblemDetails;
using Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Serilog;
using SharedKernel.Exceptions;
using SharedKernel.Exceptions.CustomProblemDetails;
using System.Reflection;
using Web;
using Web.Config;
using Web.SignalRInfra;

var builder = WebApplication.CreateBuilder(args);

var version = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
builder.Host.UseSerilog((_, config) => config.ReadFrom.Configuration(builder.Configuration)
  .WriteTo.Seq("http://localhost:5341", restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information)
  .Enrich.FromLogContext()
  .Enrich.WithProperty("AppVersion", version));

builder.Services.AddHangfire(builder.Configuration.GetConnectionString("DefaultConnection"));
builder.Services.AddInfrastructureServices(builder.Configuration, Assembly.GetExecutingAssembly());
builder.Services.AddWebServices();

builder.Services.AddControllers();
builder.Services.AddDbContext(builder.Configuration.GetConnectionString("DefaultConnection"));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
      policy => { policy.WithOrigins("http://localhost:3000", "https://localhost:7156").AllowAnyHeader().AllowAnyMethod().AllowCredentials(); });

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


if (builder.Environment.IsDevelopment())
{
}

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

builder.Services.AddSignalR().AddAzureSignalR();

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