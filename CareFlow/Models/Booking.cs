namespace CareFlow.Models;

public sealed class Booking
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public DateOnly BookingDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<BookingService> Services { get; set; } = new List<BookingService>();
}
