using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CareFlow.Data;
using CareFlow.Services;
using CareFlow.ViewModels;
using CareFlow.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CareFlow;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CareFlow");
            Directory.CreateDirectory(folder);
            var dbPath = Environment.GetEnvironmentVariable("CAREFLOW_DB_PATH");
            if (string.IsNullOrWhiteSpace(dbPath)) dbPath = Path.Combine(folder, "careflow.db");
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(dbPath))!);
            var services = new ServiceCollection();
            services.AddDbContextFactory<CareFlowDbContext>(options => options.UseSqlite($"Data Source={dbPath}"));
            services.AddSingleton<BookingService>();
            var provider = services.BuildServiceProvider();
            desktop.Exit += (_, _) => provider.Dispose();
            desktop.MainWindow = new MainWindow { DataContext = new MainViewModel(provider.GetRequiredService<BookingService>(), provider.GetRequiredService<IDbContextFactory<CareFlowDbContext>>()) };
        }
        base.OnFrameworkInitializationCompleted();
    }
}
