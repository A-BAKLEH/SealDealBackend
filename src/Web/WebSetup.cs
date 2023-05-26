using SharedKernel;
using Web.Config;
using Web.ControllerServices;
using Web.ControllerServices.QuickServices;
using Web.HTTPClients;
using Web.Outbox.Config;
using Web.Processing.ActionPlans;
using Web.Processing.Analyzer;
using Web.Processing.EmailAutomation;
using Web.Processing.Various;

namespace Web
{
    public static class WebSetup
    {
        public static void AddWebServices(this IServiceCollection services)
        {
            services.AddScoped<ActionExecuter>();
            services.AddScoped<NotifAnalyzer>();
            services.AddScoped<NotificationService>();

            services.AddScoped<AuthorizationService>();
            services.AddScoped<BrokerQService>();
            services.AddScoped<LeadQService>();
            services.AddScoped<ListingQService>();
            services.AddScoped<TagQService>();
            services.AddScoped<TemplatesQService>();
            services.AddScoped<ActionPQService>();
            services.AddScoped<AgencyQService>();
            services.AddScoped<HandleTodo>();
            services.AddScoped<APProcessor>();

            services.AddScoped<ToDoTaskQService>();
            services.AddScoped<EmailProcessor>();
            services.AddScoped<MSFTEmailQService>();
            services.AddScoped<OutboxDispatcher>();
            services.AddSingleton<IExecutionContextAccessor, ExecutionContextAccessor>();

            services.AddHttpClient<OpenAIGPT35Service>();
        }
    }
}
