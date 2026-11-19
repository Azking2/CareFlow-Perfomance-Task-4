namespace CareFlow.Services;

/// <summary>
/// Thrown when a booking request breaks a business rule (BR-01 to BR-06).
/// Its message is written for staff and is safe to show in the UI.
/// Any other exception type is treated as a technical failure and is never shown verbatim.
/// </summary>
public sealed class BookingRejectedException(string message) : Exception(message);
