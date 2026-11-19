using CareFlow.Data;
using Microsoft.EntityFrameworkCore;

namespace CareFlow.Services;

/// <summary>
/// Applies pending EF Core migrations (which also create the seed data).
/// Keeping this in the service layer means ViewModels never touch EF Core or the DbContext.
/// </summary>
public sealed class DatabaseInitializer(IDbContextFactory<CareFlowDbContext> contextFactory)
{
    public async Task InitializeAsync()
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        await db.Database.MigrateAsync();
    }
}
