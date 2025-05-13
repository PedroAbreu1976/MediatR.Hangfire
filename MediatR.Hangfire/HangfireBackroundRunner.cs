using Microsoft.Extensions.DependencyInjection;

namespace MediatR.Hangfire
{
    public class HangfireBackroundRunner(IServiceProvider serviceProvider)
    {
        public async Task RunJob(object handlerInstance, INotification notification, CancellationToken cancellationToken)
        {
            Type handlerType = handlerInstance.GetType();
            var method = handlerType.GetMethod("Handle")!;
            var genericType = typeof(INotificationHandler<>).MakeGenericType(notification.GetType());
            var obj = serviceProvider.GetRequiredService(genericType);
            var task = method.Invoke(obj, new object[] { notification, cancellationToken }) as Task;
            await task!;
        }
    }
}
