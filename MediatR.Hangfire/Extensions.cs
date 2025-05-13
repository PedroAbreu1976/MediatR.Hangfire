using Microsoft.Extensions.DependencyInjection;

namespace MediatR.Hangfire
{
    public static class Extensions
    {
        public static IServiceCollection AddHangfireNotificationPublisher(this IServiceCollection services)
        {
            services.AddSingleton<HangfireNotificationPublisher>();
            services.AddTransient<HangfireBackroundRunner>();

            return services;
        }

        public static MediatRServiceConfiguration SetHangfireNotificationPublisher(this MediatRServiceConfiguration config)
        {
            config.NotificationPublisherType = typeof(HangfireNotificationPublisher);
            return config;
        }
    }
}
