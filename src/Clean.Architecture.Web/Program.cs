using Autofac;
using Autofac.Extensions.DependencyInjection;
using Clean.Architecture.Core;
using Clean.Architecture.Infrastructure;
using Clean.Architecture.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Clean.Architecture.Web.AuthenticationAuthorization;
using Hangfire;
using Hangfire.SqlServer;

var builder = WebApplication.CreateBuilder(args);


builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

builder.Host.UseSerilog((_, config) => config.ReadFrom.Configuration(builder.Configuration));


builder.Services.AddHangfire(configuration => configuration
.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
.UseSimpleAssemblyNameTypeSerializer()
.UseRecommendedSerializerSettings()
.UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"), new SqlServerStorageOptions
{
  CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
  SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
  QueuePollInterval = TimeSpan.Zero,
  UseRecommendedIsolationLevel = true,
  DisableGlobalLocks = true
}));
builder.Services.AddHangfireServer();

/*builder.Services.Configure<CookiePolicyOptions>(options =>
{
  options.CheckConsentNeeded = context => true;
  options.MinimumSameSitePolicy = SameSiteMode.None;
});*/
builder.Services.AddControllers();

builder.Services.AddDbContext(builder.Configuration.GetConnectionString("DefaultConnection"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Services  --- put in separate file later
builder.Services.AddSingleton(typeof(AuthorizationService));



// Options --- configure each option in its own assembly

string MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
  
  options.AddPolicy(name: MyAllowSpecificOrigins,
                    policy =>
                    {
                      policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddMicrosoftIdentityWebApi(options =>
                    {
                      builder.Configuration.Bind("AzureAdB2C", options);

                      options.TokenValidationParameters.NameClaimType = "name";
                    },
            options => { builder.Configuration.Bind("AzureAdB2C", options); });


//builder.Services.AddHttpContextAccessor();
//builder.Services.AddControllersWithViews().AddNewtonsoftJson();
//builder.Services.AddRazorPages();

/*builder.Services.AddSwaggerGen(c =>
{
  c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
  c.EnableAnnotations();
});*/

// add list services for diagnostic purposes - see https://github.com/ardalis/AspNetCoreStartupServices
/*builder.Services.Configure<ServiceConfig>(config =>
{
  config.Services = new List<ServiceDescriptor>(builder.Services);

  // optional - default path to view services is /listallservices - recommended to choose your own path
  config.Path = "/listservices";
});*/

builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
  containerBuilder.RegisterModule(new DefaultCoreModule());
  containerBuilder.RegisterModule(new DefaultInfrastructureModule(builder.Environment.EnvironmentName == "Development"));
});

//builder.Logging.AddAzureWebAppDiagnostics(); add this if deploying to Azure


var app = builder.Build();

//IExecutionContextAccessor executionContextAccessor = new ExecutionContextAccessor(app.Services.GetRequiredService<IHttpContextAccessor>());


using (var scope = app.Services.CreateScope())
{
  var services = scope.ServiceProvider;

  try
  {
    var context = services.GetRequiredService<AppDbContext>();
    //context.Database.Migrate();
    //context.Database.EnsureCreated();
    //SeedData.Initialize(services);
  }
  catch (Exception ex)
  {
    var logger = services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred seeding the DB. {exceptionMessage}", ex.Message);
  }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}
else
{
  app.UseExceptionHandler();
  app.UseHsts();
}

//app.UseRouting();

app.UseHttpsRedirection();
//app.UseStaticFiles();
//app.UseCookiePolicy();

// Enable middleware to serve generated Swagger as a JSON endpoint.
//app.UseSwagger();

// Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), specifying the Swagger JSON endpoint.
//app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1"));
//app.UseSwaggerUI();

/*app.UseEndpoints(endpoints =>
{
  endpoints.MapDefaultControllerRoute();
  endpoints.MapRazorPages();
});*/
app.UseAuthentication();
app.UseAuthorization();
app.UseCors(MyAllowSpecificOrigins);
// Seed Database

app.MapControllers();
app.MapHangfireDashboard();
app.Run();

//Add-Migration InitialMigrationName -StartupProject Clean.Architecture.Web -Context AppDbContext -Project Clean.Architecture.Infrastructure
//stripe listen --forward-to https://localhost:7156/api/Webhook/webhook
