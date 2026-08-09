using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NTierTemplate.Application.Auth;
using NTierTemplate.Application.Ioc;
using NTierTemplate.Application.Users;
using NTierTemplate.Users;
using System.CommandLine;

namespace NTierTemplate.Cli;

/// <summary>
/// Base class for authenticated CLI commands.
/// </summary>
public abstract class CliCommand
{
    /// <summary>
    /// Email option for the signed-in account executing the command.
    /// </summary>
    protected Option<string> EmailOption { get; } = new("--email")
    {
        Description = "Account email address.",
        Required = true,
    };

    /// <summary>
    /// Password option for the signed-in account executing the command.
    /// </summary>
    protected Option<string> PasswordOption { get; } = new("--password")
    {
        Description = "Account password.",
        Required = true,
    };

    /// <summary>
    /// Build the System.CommandLine command tree for this handler.
    /// </summary>
    /// <returns>The configured command.</returns>
    public abstract Command Build();

    /// <summary>
    /// Attach required authentication options to a command.
    /// </summary>
    /// <param name="command">The command to configure.</param>
    protected void AddAuthOptions(Command command)
    {
        command.Options.Add(this.EmailOption);
        command.Options.Add(this.PasswordOption);
    }

    /// <summary>
    /// Run a command handler after authenticating the current user.
    /// </summary>
    /// <param name="email">Account email address.</param>
    /// <param name="password">Account password.</param>
    /// <param name="executeAsync">Command logic executed with a scoped service provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected async Task ExecuteAsAuthenticatedUserAsync(
        string email,
        string password,
        Func<IServiceProvider, Task> executeAsync,
        CancellationToken cancellationToken = default
    )
    {
        // Bootstrap a host and scoped service provider.
        using var host = CreateHost();
        using var scope = host.Services.CreateScope();

        // Authenticate the caller before running command logic.
        await this.AuthenticateAsync(scope.ServiceProvider, email, password, cancellationToken);

        await executeAsync(scope.ServiceProvider);
    }

    /// <summary>
    /// Run a command handler after authenticating an administrator.
    /// </summary>
    /// <param name="email">Administrator email address.</param>
    /// <param name="password">Administrator password.</param>
    /// <param name="executeAsync">Command logic executed with a scoped service provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected async Task ExecuteAsAdminAsync(
        string email,
        string password,
        Func<IServiceProvider, Task> executeAsync,
        CancellationToken cancellationToken = default
    )
    {
        // Bootstrap a host and scoped service provider.
        using var host = CreateHost();
        using var scope = host.Services.CreateScope();

        // Require an authenticated administrator before running command logic.
        await this.RequireAdminAsync(scope.ServiceProvider, email, password, cancellationToken);

        await executeAsync(scope.ServiceProvider);
    }

    /// <summary>
    /// Create a configured host for CLI command handlers.
    /// </summary>
    /// <returns>Configured host.</returns>
    private static IHost CreateHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                // Load CLI-specific settings and environment overrides.
                configurationBuilder
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("clisettings.json", optional: true)
                    .AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                // Register CLI principal scoping and shared application services.
                services.AddScoped<IPrincipalContainer, CliPrincipalContainer>();
                services.AddNTierTemplateApplication(context.Configuration);
            })
            .Build();
    }

    /// <summary>
    /// Validate credentials and populate the current-user context.
    /// </summary>
    /// <param name="serviceProvider">Scoped service provider for the command.</param>
    /// <param name="email">Account email address.</param>
    /// <param name="password">Account password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The authenticated user.</returns>
    private async Task<User> AuthenticateAsync(
        IServiceProvider serviceProvider,
        string email,
        string password,
        CancellationToken cancellationToken
    )
    {
        var authApplicationService = serviceProvider.GetRequiredService<IAuthApplicationService>();

        // Validate email and password against Identity.
        var user = await authApplicationService.ValidatePasswordAsync(email, password, cancellationToken);

        if (user is null)
        {
            throw new InvalidOperationException("Invalid email or password.");
        }

        return user;
    }

    /// <summary>
    /// Validate credentials, require administrator privileges, and populate the current-user context.
    /// </summary>
    /// <param name="serviceProvider">Scoped service provider for the command.</param>
    /// <param name="email">Account email address.</param>
    /// <param name="password">Account password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The authenticated administrator.</returns>
    private async Task<User> RequireAdminAsync(
        IServiceProvider serviceProvider,
        string email,
        string password,
        CancellationToken cancellationToken
    )
    {
        // Authenticate the caller first.
        var user = await this.AuthenticateAsync(serviceProvider, email, password, cancellationToken);

        // Require administrator privileges for the command.
        if (!user.IsAdmin)
        {
            throw new InvalidOperationException("Administrator privileges are required.");
        }

        return user;
    }
}
