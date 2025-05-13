using Hangfire;
using MediatR;
using MediatR.Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHangfire((sp, c) =>
        {
            c.UseInMemoryStorage();
            c.SetDataCompatibilityLevel(CompatibilityLevel.Version_180);
            c.UseColouredConsoleLogProvider();
            c.UseSimpleAssemblyNameTypeSerializer();
            c.UseRecommendedSerializerSettings();
        });
        services.AddHangfireServer();

        services.AddHangfireNotificationPublisher();
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
            cfg.SetHangfireNotificationPublisher();
        });
    })
    .Build();
await host.StartAsync();

var mediator = host.Services.GetRequiredService<IMediator>();
for (int i = 1; i < 5; i++)
{
    var cmd = new MyCommand($"Message {i}");
    var msg = await mediator.Send(cmd);
    Console.WriteLine(msg);
}

Console.WriteLine("Buying time (1 second), so the process doesn´t die...");
await Task.Delay(1000);
Console.Write("Press any key to close...");
Console.ReadKey();


public record MyEvent(string Message) : INotification;

public class MyEventHandler() : INotificationHandler<MyEvent>
{
    public async Task Handle(MyEvent notification, CancellationToken cancellationToken)
    {
        await Task.Run(() => Console.WriteLine($"RECEIVED: {notification.Message}"));
    }
}

public record MyCommand(string Message) : IRequest<string>;

public class MyCommandHandler(IMediator mediator) : IRequestHandler<MyCommand, string>
{
    public async Task<string> Handle(MyCommand command, CancellationToken cancellationToken)
    {
        var pubEvent = new MyEvent(command.Message);
        await mediator.Publish(pubEvent, cancellationToken);
        return $"SENT: {command.Message}";
    }
}