using NTierTemplate.Application.Ioc;
using NTierTemplate.Application.Users;
using NTierTemplate.Queue;
using NTierTemplate.Queue.RabbitMq;

var builder = Host.CreateApplicationBuilder(args);

// Merge optional queue-specific settings with the default host configuration.
builder.Configuration.AddJsonFile("queuesettings.json", optional: true, reloadOnChange: true);

// Register worker-specific principal scoping and shared application services.
builder.Services.AddScoped<IPrincipalContainer, QueuePrincipalContainer>();
builder.Services.AddNTierTemplateApplication(builder.Configuration);

// Run RabbitMQ consumption and failed-command retry loops.
builder.Services.AddHostedService<QueueConsumerService>();
builder.Services.AddHostedService<FailedCommandRetryService>();

var host = builder.Build();

// Ensure Identity roles exist before processing commands.
await NTierTemplateStartupExtensions.EnsureDefaultRolesAsync(host.Services);

await host.RunAsync();
