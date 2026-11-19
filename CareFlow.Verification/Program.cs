using CareFlow.Data;
using CareFlow.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

var dbPath = Path.Combine(Path.GetTempPath(), $"careflow-check-{Guid.NewGuid():N}.db");
var factory = new SqliteFactory(dbPath);
var service = new BookingService(factory);
var date = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
var passed = new List<string>();

try
{
    await new DatabaseInitializer(factory).InitializeAsync();

    var initial = await service.GetAvailabilityAsync(date);
    Check(initial.Count == 4 && initial.All(x => x.Remaining == x.DailyCapacity), "AC-01 initial availability uses persisted capacities");

    await service.CreateBookingAsync(1, date, new[] { 1, 2 });
    var afterMulti = await service.GetAvailabilityAsync(date);
    Check(afterMulti.Single(x => x.ServiceId == 1).Booked == 1 && afterMulti.Single(x => x.ServiceId == 2).Booked == 1,
        "AC-02 one booking stores all selected services");

    var beforeInvalid = (await service.GetBookingsAsync()).Count;
    await ExpectRejected(() => service.CreateBookingAsync(1, date, Array.Empty<int>()));
    await ExpectRejected(() => service.CreateBookingAsync(1, date, new[] { 1, 1 }));
    await ExpectRejected(() => service.CreateBookingAsync(1, DateOnly.FromDateTime(DateTime.Today.AddDays(-1)), new[] { 1 }));
    Check((await service.GetBookingsAsync()).Count == beforeInvalid, "AC-03 invalid requests create no booking");

    await using (var db = await factory.CreateDbContextAsync())
    {
        var ecg = await db.DiagnosticServices.SingleAsync(x => x.Id == 4);
        ecg.DailyCapacity = 1;
        await db.SaveChangesAsync();
    }
    var stale = (await service.GetAvailabilityAsync(date)).Single(x => x.ServiceId == 4);
    Check(stale.Remaining == 1, "AC-04 initially displayed capacity is available");
    await service.CreateBookingAsync(2, date, new[] { 4 });
    await ExpectRejected(() => service.CreateBookingAsync(3, date, new[] { 4 }));
    Check((await service.GetAvailabilityAsync(date)).Single(x => x.ServiceId == 4).Remaining == 0,
        "AC-04 recheck blocks a service that filled after availability was displayed");

    var beforeFailure = (await service.GetBookingsAsync()).Count;
    await using (var db = await factory.CreateDbContextAsync())
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER FailBookingService BEFORE INSERT ON BookingServices BEGIN SELECT RAISE(ABORT, 'controlled verification failure'); END;");
    try
    {
        await service.CreateBookingAsync(1, date, new[] { 1, 3 });
        throw new Exception("Expected the controlled database failure.");
    }
    catch (DbUpdateException) { }
    Check((await service.GetBookingsAsync()).Count == beforeFailure,
        "AC-05 failed service insert rolls back its booking header and service rows");

    var restartedService = new BookingService(new SqliteFactory(dbPath));
    var persisted = await restartedService.GetBookingsAsync();
    Check(persisted.Count == beforeFailure && persisted.Any(x => x.PatientName == "Alex Santos" && x.Services.Contains("Urinalysis")),
        "AC-06 bookings remain available through a fresh database factory");

    Console.WriteLine("CareFlow verification passed:");
    foreach (var item in passed) Console.WriteLine($"PASS  {item}");
}
finally
{
    SqliteConnection.ClearAllPools();
    if (File.Exists(dbPath)) File.Delete(dbPath);
    if (File.Exists(dbPath + "-shm")) File.Delete(dbPath + "-shm");
    if (File.Exists(dbPath + "-wal")) File.Delete(dbPath + "-wal");
}

void Check(bool condition, string description)
{
    if (!condition) throw new Exception($"Verification failed: {description}");
    passed.Add(description);
}

static async Task ExpectRejected(Func<Task> action)
{
    try { await action(); }
    catch (InvalidOperationException) { return; }
    throw new Exception("Expected the invalid request to be rejected.");
}

sealed class SqliteFactory(string path) : IDbContextFactory<CareFlowDbContext>
{
    private readonly DbContextOptions<CareFlowDbContext> _options = new DbContextOptionsBuilder<CareFlowDbContext>()
        .UseSqlite($"Data Source={path}").Options;
    public CareFlowDbContext CreateDbContext() => new(_options);
    public Task<CareFlowDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(CreateDbContext());
}
