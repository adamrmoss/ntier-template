using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace NTierTemplate.Data;

/// <summary>
/// Design-time factory for creating NTierTemplateDbContext instances. Used by EF Core tools (migrations, scaffolding).
/// </summary>
public class NTierTemplateDbContextFactory : IDesignTimeDbContextFactory<NTierTemplateDbContext>
{
    /// <summary>
    /// Create a DbContext configured from dbsettings.json for design-time operations.
    /// </summary>
    /// <param name="args">Command-line arguments (unused; configuration comes from dbsettings.json).</param>
    /// <returns>A configured NTierTemplateDbContext instance.</returns>
    public NTierTemplateDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("dbsettings.json")
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<NTierTemplateDbContext>();
        optionsBuilder.UseMySql(
            config.GetConnectionString("DefaultConnection")!,
            new MySqlServerVersion(new Version(8, 0, 35)),
            mySqlOptions =>
            {
                mySqlOptions.EnablePrimitiveCollectionsSupport(true);
                mySqlOptions.TranslateParameterizedCollectionsToConstants();
            });

        return new NTierTemplateDbContext(optionsBuilder.Options);
    }
}
