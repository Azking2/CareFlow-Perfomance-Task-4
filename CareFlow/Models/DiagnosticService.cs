namespace CareFlow.Models;

public sealed class DiagnosticService
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DailyCapacity { get; set; }
    public ICollection<BookingService> BookingServices { get; set; } = new List<BookingService>();
}
