namespace Restaurant.Models;

public class MonthlyStatRow
{
    public int Month { get; set; }
    public decimal TotalRevenue { get; set; }
    public int CompletedReservations { get; set; }
    public int TotalGuests { get; set; }
}