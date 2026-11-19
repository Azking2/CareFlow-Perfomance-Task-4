namespace CareFlow.Models;

public sealed class Patient
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
