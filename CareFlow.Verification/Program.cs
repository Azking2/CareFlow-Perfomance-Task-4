using CareFlow.Data;
using CareFlow.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

var dbPath = Path.Combine(Path.GetTempPath(), $"careflow-check-{Guid.NewGuid():N}.db");
var factory = new SqliteFactory(dbPath);
var service = new BookingService(factory);
var date = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
var otherDate = date.AddDays(1);
var passed = new List<string>();

try
{
    await new DatabaseInitializer(factory).InitializeAsync();

    // ---- AC-01: availability comes from persisted data -------------------------------------
    var initial = await service.GetAvailabilityAsync(date);
    Check(initial.Count == 4 && initial.All(x => x.Remaining == x.DailyCapacity),
        "AC-01 initial availability uses persisted capacities");

    // ---- AC-02 / BR-04: one booking, all selected services, one unit each -------------------
    await service.CreateBookingAsync(1, date, new[] { 1, 2 });
    var afterMulti = await service.GetAvailabilityAsync(date);
    Check(afterMulti.Single(x => x.ServiceId == 1).Booked == 1 && afterMulti.Single(x => x.ServiceId == 2).Booked == 1,
        "AC-02 each selected service consumes exactly one unit of capacity");
    Check(await CountRowsAsync() == (1, 2),
        "AC-02 one booking row and two booking-service rows were stored");
    Check((await service.GetAvailabilityAsync(otherDate)).All(x => x.Booked == 0),
        "AC-01 availability is calculated per date (other date is untouched)");

    // ---- AC-03: invalid requests create nothing (BR-01, BR-02, BR-03, BR-06) -----------------
    var rowsBeforeInvalid = await CountRowsAsync();
    var snapshotBeforeInvalid = Snapshot(await service.GetAvailabilityAsync(date));
    await ExpectRejected(() => service.CreateBookingAsync(1, date, Array.Empty<int>()));                                   // BR-02: none
    await ExpectRejected(() => service.CreateBookingAsync(1, date, new[] { 1, 2, 3, 4 }));                                // BR-02: four
    await ExpectRejected(() => service.CreateBookingAsync(1, date, new[] { 1, 1 }));                                      // BR-06: duplicate
    await ExpectRejected(() => service.CreateBookingAsync(1, DateOnly.FromDateTime(DateTime.Today.AddDays(-1)), new[] { 1 })); // BR-03: past date
    await ExpectRejected(() => service.CreateBookingAsync(999, date, new[] { 1 }));                                       // BR-01: unknown patient
    await ExpectRejected(() => service.CreateBookingAsync(1, date, new[] { 1, 999 }));                                   // unknown service
    Check(await CountRowsAsync() == rowsBeforeInvalid && Snapshot(await service.GetAvailabilityAsync(date)) == snapshotBeforeInvalid,
        "AC-03 invalid requests (0 or 4 services, duplicate, past date, unknown patient, unknown service) change nothing");

    // ---- AC-04: stale availability is rechecked at confirmation (BR-05, BR-08) --------------
    await using (var db = await factory.CreateDbContextAsync())
    {
        var ecg = await db.DiagnosticServices.SingleAsync(x => x.Id == 4);
        ecg.DailyCapacity = 1; // shrink ECG to one spot so the scenario is quick to set up
        await db.SaveChangesAsync();
    }
    var stale = (await service.GetAvailabilityAsync(date)).Single(x => x.ServiceId == 4);
    Check(stale.Remaining == 1, "AC-04 ECG is displayed as available (this is the 'stale' view)");
    await service.CreateBookingAsync(2, date, new[] { 4 }); // another user takes the last ECG spot
    await ExpectRejected(() => service.CreateBookingAsync(3, date, new[] { 4 })); // booking made from the stale view
    Check((await service.GetAvailabilityAsync(date)).Single(x => x.ServiceId == 4).Remaining == 0,
        "AC-04 the recheck rejects the stale request and ECG is not overbooked");

    // ---- AC-03 (fully booked service): the whole request is rejected, nothing partial -------
    var rowsBeforeFull = await CountRowsAsync();
    var snapshotBeforeFull = Snapshot(await service.GetAvailabilityAsync(date));
    await ExpectRejected(() => service.CreateBookingAsync(3, date, new[] { 1, 4 })); // CBC is free, ECG is full
    Check(await CountRowsAsync() == rowsBeforeFull && Snapshot(await service.GetAvailabilityAsync(date)) == snapshotBeforeFull,
        "AC-03 a fully booked service rejects the whole request (the free service is not booked either)");

    // ---- AC-05: a database failure mid-booking leaves nothing behind ------------------------
    var rowsBeforeFailure = await CountRowsAsync();
    var snapshotBeforeFailure = Snapshot(await service.GetAvailabilityAsync(date));
    await using (var db = await factory.CreateDbContextAsync())
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER FailBookingService BEFORE INSERT ON BookingServices BEGIN SELECT RAISE(ABORT, 'controlled verification failure'); END;");
    try
    {
        await service.CreateBookingAsync(1, date, new[] { 1, 3 });
        throw new Exception("Expected the controlled database failure.");
    }
    catch (DbUpdateException) { }
    Check(await CountRowsAsync() == rowsBeforeFailure,
        "AC-05 failed service insert leaves no booking row and no booking-service rows");
    Check(Snapshot(await service.GetAvailabilityAsync(date)) == snapshotBeforeFailure,
        "AC-05 failed booking does not change any service's capacity");

    await using (var db = await factory.CreateDbContextAsync())
        await db.Database.ExecuteSqlRawAsync("DROP TRIGGER FailBookingService;");
    await service.CreateBookingAsync(1, date, new[] { 1, 3 });
    Check((await service.GetAvailabilityAsync(date)).Single(x => x.ServiceId == 3).Booked == 1,
        "AC-05 the same request succeeds once the failure is removed (no capacity was lost)");

    // ---- AC-06: data survives a restart (new factory, same database file) -------------------
    var expectedBookings = (await service.GetBookingsAsync()).Count;
    var restartedService = new BookingService(new SqliteFactory(dbPath));
    var persisted = await restartedService.GetBookingsAsync();
    Check(persisted.Count == expectedBookings
          && persisted.Any(x => x.PatientName == "Alex Santos" && x.Services.Contains("Urinalysis"))
          && persisted.Any(x => x.PatientName == "Alex Santos" && x.Services.Contains("Chest X-Ray")),
        "AC-06 bookings and their services are still there through a fresh database factory");

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

// Counts rows straight from the tables (not through BookingService) so orphaned rows would be noticed.
async Task<(int Bookings, int Lines)> CountRowsAsync()
{
    await using var db = await factory.CreateDbContextAsync();
    return (await db.Bookings.CountAsync(), await db.BookingServices.CountAsync());
}

static string Snapshot(IEnumerable<ServiceAvailability> availability) =>
    string.Join(",", availability.OrderBy(x => x.ServiceId).Select(x => $"{x.ServiceId}:{x.Booked}"));

static async Task ExpectRejected(Func<Task> action)
{
    try { await action(); }
    catch (BookingRejectedException) { return; }
    throw new Exception("Expected the invalid request to be rejected.");
}

sealed class SqliteFactory(string path) : IDbContextFactory<CareFlowDbContext>
{
    private readonly DbContextOptions<CareFlowDbContext> _options = new DbContextOptionsBuilder<CareFlowDbContext>()
        .UseSqlite($"Data Source={path}").Options;
    public CareFlowDbContext CreateDbContext() => new(_options);
    public Task<CareFlowDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(CreateDbContext());
}
