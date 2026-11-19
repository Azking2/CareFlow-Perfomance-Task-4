using CareFlow.Models;
using Microsoft.EntityFrameworkCore;

namespace CareFlow.Data;

public sealed class CareFlowDbContext(DbContextOptions<CareFlowDbContext> options) : DbContext(options)
{
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<DiagnosticService> DiagnosticServices => Set<DiagnosticService>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingService> BookingServices => Set<BookingService>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Patient>().HasKey(x => x.Id);
        modelBuilder.Entity<Patient>().Property(x => x.FullName).HasMaxLength(120).IsRequired();

        modelBuilder.Entity<DiagnosticService>().HasKey(x => x.Id);
        modelBuilder.Entity<DiagnosticService>().Property(x => x.Name).HasMaxLength(80).IsRequired();
        modelBuilder.Entity<DiagnosticService>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<DiagnosticService>().ToTable(t => t.HasCheckConstraint("CK_DiagnosticService_Capacity", "DailyCapacity > 0"));

        modelBuilder.Entity<Booking>().HasKey(x => x.Id);
        modelBuilder.Entity<Booking>().HasOne(x => x.Patient).WithMany(x => x.Bookings).HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Booking>().HasIndex(x => x.BookingDate);

        modelBuilder.Entity<BookingService>().HasKey(x => new { x.BookingId, x.DiagnosticServiceId });
        modelBuilder.Entity<BookingService>().HasOne(x => x.Booking).WithMany(x => x.Services).HasForeignKey(x => x.BookingId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<BookingService>().HasOne(x => x.DiagnosticService).WithMany(x => x.BookingServices).HasForeignKey(x => x.DiagnosticServiceId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Patient>().HasData(
            new Patient { Id = 1, FullName = "Alex Santos" },
            new Patient { Id = 2, FullName = "Jamie Cruz" },
            new Patient { Id = 3, FullName = "Taylor Reyes" });
        modelBuilder.Entity<DiagnosticService>().HasData(
            new DiagnosticService { Id = 1, Name = "Complete Blood Count", DailyCapacity = 10 },
            new DiagnosticService { Id = 2, Name = "Urinalysis", DailyCapacity = 8 },
            new DiagnosticService { Id = 3, Name = "Chest X-Ray", DailyCapacity = 6 },
            new DiagnosticService { Id = 4, Name = "ECG", DailyCapacity = 5 });
    }
}
