namespace NTierTemplate.Data;

/// <summary>
/// Base class for data access objects.
/// </summary>
public class DaoBase
{
    /// <summary>
    /// Database context shared by DAO operations.
    /// </summary>
    protected readonly NTierTemplateDbContext dbContext;

    /// <summary>
    /// Create a DAO bound to the given database context.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    protected DaoBase(NTierTemplateDbContext dbContext)
    {
        this.dbContext = dbContext;
    }
}
