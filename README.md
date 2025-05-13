# Hangfire MediatR Notification Publisher SDK

[![NuGet version (YourNamespace.Hangfire.MediatR.Publisher)](https://img.shields.io/nuget/v/YourNamespace.Hangfire.MediatR.Publisher.svg?style=flat-square)](https://www.nuget.org/packages/YourNamespace.Hangfire.MediatR.Publisher/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

This SDK provides a seamless way to publish MediatR `INotification`s to Hangfire, allowing them to be processed as background jobs. This is particularly useful for:

*   Offloading notification handling to a background process.
*   Ensuring reliable delivery and execution of notifications with Hangfire's retry mechanisms.
*   Handling long-running notification tasks without blocking the main application thread.

## Features

*   **Easy Integration:** Simple extension methods to wire up MediatR and Hangfire.
*   **Decoupled Processing:** Publish notifications and let Hangfire take care of their execution.
*   **Reliability:** Leverages Hangfire's persistence, retries, and dashboard for monitoring.
*   **Asynchronous by Default:** Notifications are processed in the background.
*   **Standard MediatR:** Continue using standard MediatR `INotification` and `INotificationHandler<T>` patterns.

## Installation

Install the NuGet package into your ASP.NET Core application:

```bash
dotnet add package YourNamespace.Hangfire.MediatR.Publisher


Or via the NuGet Package Manager Console:

Install-Package YourNamespace.Hangfire.MediatR.Publisher
IGNORE_WHEN_COPYING_START
content_copy
download
Use code with caution.
Powershell
IGNORE_WHEN_COPYING_END
Prerequisites

You must have Hangfire already configured in your application (e.g., services.AddHangfire(...), services.AddHangfireServer()).

You must have MediatR configured (e.g., services.AddMediatR(...)).

Usage

Configure Services:
In your Program.cs or Startup.cs, use the provided extension methods:

AddHangfireNotificationPublisher(): Registers the necessary services for the publisher.

SetHangfireNotificationPublisher(): Configures MediatR to use the Hangfire publisher.

// Program.cs
using Hangfire;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
// using YourNamespace.Hangfire.MediatR.Publisher; // Add your SDK's namespace

public class Program
{
    public static async Task Main(string[] args)
    {
        using var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                // 1. Configure Hangfire (example with In-Memory storage)
                services.AddHangfire((sp, c) =>
                {
                    c.UseInMemoryStorage(); // Replace with your preferred storage
                    c.SetDataCompatibilityLevel(CompatibilityLevel.Version_180);
                    c.UseColouredConsoleLogProvider();
                    c.UseSimpleAssemblyNameTypeSerializer();
                    c.UseRecommendedSerializerSettings();
                });
                services.AddHangfireServer(options =>
                {
                    options.WorkerCount = 2; // Example: set worker count
                });

                // 2. Add the Hangfire Notification Publisher
                services.AddHangfireNotificationPublisher(); // <--- SDK Integration

                // 3. Configure MediatR
                services.AddMediatR(cfg =>
                {
                    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
                    // 4. Set MediatR to use the Hangfire Publisher
                    cfg.SetHangfireNotificationPublisher(); // <--- SDK Integration
                });
            })
            .Build();

        await host.StartAsync(); // Start Hangfire server and other hosted services

        var mediator = host.Services.GetRequiredService<IMediator>();

        Console.WriteLine("Publishing notifications via MediatR to Hangfire...");
        for (int i = 1; i < 5; i++)
        {
            var cmd = new MyCommand($"Message {i}");
            // This Send will trigger MyCommandHandler, which then publishes MyEvent
            var msg = await mediator.Send(cmd);
            Console.WriteLine(msg);
        }

        Console.WriteLine("Notifications enqueued to Hangfire. Check Hangfire Dashboard (if configured).");
        Console.WriteLine("Handlers will be processed by Hangfire workers.");
        Console.WriteLine("Buying time for Hangfire to process (5 seconds)...");
        await Task.Delay(5000); // Give Hangfire time to process

        Console.Write("Press any key to close...");
        Console.ReadKey();

        await host.StopAsync();
    }
}

// --- Your MediatR Definitions (Notifications, Handlers, Requests) ---

// 1. Define your Notification
public record MyEvent(string Message) : INotification;

// 2. Define your Notification Handler
// This handler will be executed by a Hangfire background job
public class MyEventHandler() : INotificationHandler<MyEvent>
{
    public async Task Handle(MyEvent notification, CancellationToken cancellationToken)
    {
        // Simulate work
        await Task.Delay(200, cancellationToken);
        Console.WriteLine($"[Hangfire Worker] RECEIVED VIA HANGFIRE: {notification.Message} on Thread {Thread.CurrentThread.ManagedThreadId}");
    }
}

// Example: A command that publishes a notification
public record MyCommand(string Message) : IRequest<string>;

public class MyCommandHandler(IMediator mediator) : IRequestHandler<MyCommand, string>
{
    public async Task<string> Handle(MyCommand command, CancellationToken cancellationToken)
    {
        var pubEvent = new MyEvent(command.Message);
        // Publishing the event here will be intercepted by the SDK
        // and enqueued to Hangfire instead of being handled immediately in-process.
        await mediator.Publish(pubEvent, cancellationToken);
        return $"SENT TO HANGFIRE QUEUE: {command.Message}";
    }
}
IGNORE_WHEN_COPYING_START
content_copy
download
Use code with caution.
C#
IGNORE_WHEN_COPYING_END

Define Notifications and Handlers:
Create your MediatR INotifications and their corresponding INotificationHandler<T>s as you normally would.

// Notification
public record MyEvent(string Message) : INotification;

// Notification Handler
public class MyEventHandler : INotificationHandler<MyEvent>
{
    public async Task Handle(MyEvent notification, CancellationToken cancellationToken)
    {
        // This code will run inside a Hangfire background job
        Console.WriteLine($"[Hangfire Worker] RECEIVED VIA HANGFIRE: {notification.Message}");
        await Task.CompletedTask;
    }
}
IGNORE_WHEN_COPYING_START
content_copy
download
Use code with caution.
C#
IGNORE_WHEN_COPYING_END

Publish Notifications:
Use IMediator.Publish(notification) as usual. The SDK will intercept the call and enqueue the notification processing to Hangfire.

public class MyService
{
    private readonly IMediator _mediator;

    public MyService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task DoSomethingAndNotifyAsync(string messageContent)
    {
        var myEvent = new MyEvent($"Important: {messageContent}");
        await _mediator.Publish(myEvent); // This will be handled by Hangfire
        Console.WriteLine($"Event '{myEvent.Message}' enqueued for Hangfire processing.");
    }
}
IGNORE_WHEN_COPYING_START
content_copy
download
Use code with caution.
C#
IGNORE_WHEN_COPYING_END
How It Works

When IMediator.Publish(notification) is called, the HangfireNotificationPublisher (registered via SetHangfireNotificationPublisher()) intercepts the call.

Instead of invoking handlers directly, it serializes the INotification object.

It then enqueues a Hangfire background job, passing the serialized notification and its type.

A Hangfire worker picks up this job.

Inside the Hangfire job, a dedicated internal handler (HangfireMediatorBridge) deserializes the notification.

This bridge then uses an IMediator instance (scoped to the Hangfire job) to Publish the deserialized notification again. This time, because it's within the Hangfire job's context (and not intercepted by the Hangfire publisher again), MediatR dispatches it to the actual INotificationHandler<T>s registered in your application.

This ensures that your original notification handlers are executed within the context of a Hangfire background job, benefiting from Hangfire's features.

Configuration Options

Currently, the SDK relies on Hangfire's and MediatR's own configuration mechanisms.
Ensure your Hangfire instance is properly configured with:

Persistent Storage: For production, use a persistent storage option like SQL Server, Redis, etc., instead of UseInMemoryStorage().

Serialization Settings: The SDK uses Hangfire's configured serializer. UseRecommendedSerializerSettings() with UseSimpleAssemblyNameTypeSerializer() is generally a good practice.

Contributing

Contributions are welcome! Please feel free to submit a Pull Request or open an Issue.

Fork the repository.

Create your feature branch (git checkout -b feature/AmazingFeature).

Commit your changes (git commit -m 'Add some AmazingFeature').

Push to the branch (git push origin feature/AmazingFeature).

Open a Pull Request.

License

This project is licensed under the MIT License - see the LICENSE file for details.
(You'll need to add a LICENSE.md file with the MIT license text to your repository).
