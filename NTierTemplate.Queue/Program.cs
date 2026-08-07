using NTierTemplate.Application.Ioc;
using NTierTemplate.Application.Users;
using NTierTemplate.Queue;
using NTierTemplate.Queue.RabbitMq;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddJsonFile("queuesettings.json", optional: true, reloadOnChange: true);

builder.Services.AddScoped<IPrincipalContainer, QueuePrincipalContainer>();
builder.Services.AddNTierTemplateApplication(builder.Configuration);
builder.Services.AddHostedService<QueueConsumerService>();
builder.Services.AddHostedService<FailedCommandRetryService>();

var host = builder.Build();

await NTierTemplateStartupExtensions.EnsureDefaultRolesAsync(host.Services);

await host.RunAsync();
