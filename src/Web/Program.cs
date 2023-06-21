﻿using Core.Constants;
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
using Web.RealTimeNotifs;

var builder = WebApplication.CreateBuilder(args);

var version = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

builder.Host.UseSerilog((_, config) => config.ReadFrom.Configuration(builder.Configuration)
  .Enrich.FromLogContext()
  .Enrich.WithProperty("AppVersion", version));

string PostgresconnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var hangfireConnectionString = builder.Configuration.GetConnectionString("HangfireConnection");

builder.Services.AddHangfire(hangfireConnectionString);
builder.Services.AddInfrastructureServices(builder.Configuration, Assembly.GetExecutingAssembly());
builder.Services.AddWebServices();
builder.Services.AddControllers();
builder.Services.AddDbContext(PostgresconnectionString);
builder.Services.AddDbContextFactory(PostgresconnectionString);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
      policy => { policy.WithOrigins("http://localhost:3000", "https://localhost:7156")
          .AllowAnyHeader().AllowAnyMethod().AllowCredentials(); });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddMicrosoftIdentityWebApi(options =>
                    {
                        builder.Configuration.Bind("AzureAdB2C", options);
                        options.TokenValidationParameters.NameClaimType = "name";
                        options.Events = new JwtBearerEvents
                        {
                            OnMessageReceived = context =>
                            {
                                var accessToken = context.Request.Query["access_token"];

                                // If the request is for our hub...
                                var path = context.HttpContext.Request.Path;
                                if (!string.IsNullOrEmpty(accessToken) &&
                                    //(path.StartsWithSegments("/hubs/notifs")))
                                    (path.StartsWithSegments("/notifs")))
                                {
                                    // Read the token out of the query string
                                    context.Token = accessToken;
                                }
                                return Task.CompletedTask;
                            }
                        };
                    },
                    options =>
                    {
                        builder.Configuration.Bind("AzureAdB2C", options);
                    });

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
builder.Services.AddSignalR().AddAzureSignalR();

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