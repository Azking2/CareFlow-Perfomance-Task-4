using System.Data;
using CareFlow.Data;
using CareFlow.Models;
using Microsoft.EntityFrameworkCore;

namespace CareFlow.Services;

public sealed record PatientOption(int Id, string FullName);

public sealed record ServiceAvailability(int ServiceId, string Name, int DailyCapacity, int Booked, int Remaining);

public sealed record BookingListItem(int Id, string PatientName, DateOnly Date, string Services)
{
    public string Display => $"#{Id}  {Date:MMM d, yyyy}  {PatientName}  |  {Services}";
}

public sealed class BookingService(IDbContextFactory<CareFlowDbContext> contextFactory)
{
    public async Task<List<PatientOption>> GetPatientsAsync()
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        return await db.Patients.AsNoTracking().OrderBy(x => x.FullName)
            .Select(x => new PatientOption(x.Id, x.FullName)).ToListAsync();
    }

    public async Task<List<ServiceAvailability>> GetAvailabilityAsync(DateOnly date)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        return await db.DiagnosticServices.AsNoTracking().OrderBy(x => x.Name)
            .Select(service => new ServiceAvailability(service.Id, service.Name, service.DailyCapacity,
                db.BookingServices.Count(line => line.DiagnosticServiceId == service.Id && line.Booking.BookingDate == date),
                service.DailyCapacity - db.BookingServices.Count(line => line.DiagnosticServiceId == service.Id && line.Booking.BookingDate == date)))
            .ToListAsync();
    }

    public async Task<List<BookingListItem>> GetBookingsAsync()
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var bookings = await db.Bookings.AsNoTracking().Include(x => x.Patient)
            .Include(x => x.Services).ThenInclude(x => x.DiagnosticService)
            .OrderByDescending(x => x.BookingDate).ThenByDescending(x => x.Id).ToListAsync();
        return bookings.Select(x => new BookingListItem(x.Id, x.Patient.FullName, x.BookingDate,
            string.Join(", ", x.Services.OrderBy(s => s.DiagnosticService.Name).Select(s => s.DiagnosticService.Name)))).ToList();
    }

    public async Task CreateBookingAsync(int patientId, DateOnly date, IReadOnlyCollection<int> serviceIds)
    {
        if (date < DateOnly.FromDateTime(DateTime.Now)) throw new InvalidOperationException("The booking date cannot be in the past.");
        if (serviceIds.Count is < 1 or > 3) throw new InvalidOperationException("Choose one to three services.");
        if (serviceIds.Distinct().Count() != serviceIds.Count) throw new InvalidOperationException("Each service can only be selected once.");

        await using var db = await contextFactory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (!await db.Patients.AnyAsync(x => x.Id == patientId)) throw new InvalidOperationException("Select an existing patient.");

        var services = await db.DiagnosticServices.Where(x => serviceIds.Contains(x.Id)).ToListAsync();
        if (services.Count != serviceIds.Count) throw new InvalidOperationException("One or more selected services are unavailable.");

        foreach (var service in services)
        {
            var booked = await db.BookingServices.CountAsync(x => x.DiagnosticServiceId == service.Id && x.Booking.BookingDate == date);
            if (booked >= service.DailyCapacity)
                throw new InvalidOperationException($"{service.Name} is fully booked for {date:MMM d, yyyy}. Refresh availability and choose another service or date.");
        }

        var booking = new Booking { PatientId = patientId, BookingDate = date, CreatedAt = DateTime.Now };
        foreach (var service in services) booking.Services.Add(new Models.BookingService { DiagnosticServiceId = service.Id });
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }
}
