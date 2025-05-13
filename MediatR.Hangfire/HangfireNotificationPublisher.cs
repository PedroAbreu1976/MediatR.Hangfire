using Hangfire;

namespace MediatR.Hangfire
{
    public class HangfireNotificationPublisher : INotificationPublisher
    {
        public Task Publish(
            IEnumerable<NotificationHandlerExecutor> handlerExecutors,
            INotification notification,
            CancellationToken cancellationToken)
        {
            foreach (var handler in handlerExecutors)
            {
                BackgroundJob.Enqueue<HangfireBackroundRunner>(runner => runner.RunJob(handler.HandlerInstance, notification, cancellationToken));
            }
            return Task.CompletedTask;
        }
    }
}
