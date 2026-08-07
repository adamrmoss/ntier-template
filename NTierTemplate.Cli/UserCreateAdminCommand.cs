using Microsoft.Extensions.DependencyInjection;
using NTierTemplate.Application.Users;
using NTierTemplate.Users;
using System.CommandLine;

namespace NTierTemplate.Cli;

/// <summary>
/// Administrator creation CLI command.
/// </summary>
public sealed class UserCreateAdminCommand : CliCommand
{
    private readonly Option<string> adminEmailOption = new("--admin-email")
    {
        Description = "Email address for the new administrator account.",
        Required = true,
    };

    private readonly Option<string> adminPasswordOption = new("--admin-password")
    {
        Description = "Password for the new administrator account.",
        Required = true,
    };

    /// <inheritdoc />
    public override Command Build()
    {
        var command = new Command("user", "User administration tasks.");
        var createAdminCommand = new Command("create-admin", "Create an administrator account.");

        this.AddAuthOptions(createAdminCommand);
        createAdminCommand.Options.Add(this.adminEmailOption);
        createAdminCommand.Options.Add(this.adminPasswordOption);

        createAdminCommand.SetAction(async parseResult =>
        {
            try
            {
                await this.CreateAdminUserAsync(
                    parseResult.GetValue(this.EmailOption)!,
                    parseResult.GetValue(this.PasswordOption)!,
                    parseResult.GetValue(this.adminEmailOption)!,
                    parseResult.GetValue(this.adminPasswordOption)!
                );

                return 0;
            }
            catch (InvalidOperationException exception)
            {
                Console.Error.WriteLine(exception.Message);
                return 1;
            }
        });

        command.Subcommands.Add(createAdminCommand);

        return command;
    }

    /// <summary>
    /// Create an administrator account.
    /// </summary>
    /// <param name="email">Authenticated administrator email.</param>
    /// <param name="password">Authenticated administrator password.</param>
    /// <param name="adminEmail">New administrator email.</param>
    /// <param name="adminPassword">New administrator password.</param>
    private async Task CreateAdminUserAsync(
        string email,
        string password,
        string adminEmail,
        string adminPassword
    )
    {
        await this.ExecuteAsAdminAsync(email, password, async serviceProvider =>
        {
            var userApplicationService = serviceProvider.GetRequiredService<IUserApplicationService>();
            var result = await userApplicationService.CreateAdminAsync(adminEmail, adminPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors);
                throw new InvalidOperationException("Failed to create administrator: " + errors);
            }

            Console.WriteLine("Administrator created for {0}.", adminEmail);
        });
    }
}
