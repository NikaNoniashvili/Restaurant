namespace Restaurant.Models;

public class CreateReservationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int? ReservationId { get; set; }
}