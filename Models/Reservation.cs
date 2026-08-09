namespace Restaurant.Models;

public class Reservation
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime ReservationDate { get; set; }
    public TimeSpan ReservationTime { get; set; }
    public int GuestCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PrepaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string ReservationStatus { get; set; } = "Active";
    public DateTime CreatedAt { get; set; }

    public bool IsActive { get; set; } = true;
    public string DepositPaymentStatus { get; set; } = "Unpaid";
    public string FinalPaymentStatus { get; set; } = "Unpaid";

    public List<ReservationItem> Items { get; set; } = new();
}