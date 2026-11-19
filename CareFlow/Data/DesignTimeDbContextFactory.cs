using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CareFlow.Data;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CareFlowDbContext>
{
    public CareFlowDbContext CreateDbContext(string[] args)
    {
        var path = Environment.GetEnvironmentVariable("CAREFLOW_DB_PATH");
        if (string.IsNullOrWhiteSpace(path))
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CareFlow");
            Directory.CreateDirectory(folder);
            path = Path.Combine(folder, "careflow.db");
        }
        var options = new DbContextOptionsBuilder<CareFlowDbContext>()
            .UseSqlite($"Data Source={path}")
            .Options;
        return new CareFlowDbContext(options);
    }
}
