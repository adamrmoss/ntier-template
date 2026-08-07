using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NTierTemplate.Application.Email;
using NTierTemplate.Data;
using NTierTemplate.Data.Users;

namespace NTierTemplate.Application.Ioc;

/// <summary>
/// Shared dependency injection wiring for NTierTemplate entry-point hosts.
/// </summary>
public static class NTierTemplateApplicationExtensions
{
    /// <summary>
    /// Register serializers, DAOs, application services, EF Core, and Identity.
    /// </summary>
    /// <param name="serviceCollection">The service collection to configure.</param>
    /// <param name="configuration">Host configuration (connection strings, etc.).</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddNTierTemplateApplication(
        this IServiceCollection serviceCollection,
        IConfiguration configuration
    )
    {
        serviceCollection.Configure<AppOptions>(configuration.GetSection(AppOptions.SectionName));
        serviceCollection.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));

        serviceCollection.RegisterSerializers();
        serviceCollection.RegisterDaos();
        serviceCollection.RegisterServices();

        serviceCollection.AddDbContext<NTierTemplateDbContext>(options =>
        {
            options.UseMySql(
                configuration.GetConnectionString("DefaultConnection"),
                new MySqlServerVersion(new Version(8, 0, 35)),
                mySqlOptions =>
                {
                    mySqlOptions.EnablePrimitiveCollectionsSupport(true);
                    mySqlOptions.TranslateParameterizedCollectionsToConstants();
                }
            );
        });

        serviceCollection
            .AddIdentityCore<ApplicationUser>(identityOptions =>
            {
                identityOptions.User.RequireUniqueEmail = true;
                identityOptions.Password.RequiredLength = 8;
                identityOptions.Password.RequireDigit = true;
                identityOptions.Password.RequireLowercase = true;
                identityOptions.Password.RequireUppercase = true;
                identityOptions.Password.RequireNonAlphanumeric = false;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<NTierTemplateDbContext>()
            .AddDefaultTokenProviders();

        return serviceCollection;
    }
}
