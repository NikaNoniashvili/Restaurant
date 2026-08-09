namespace Restaurant.Models;

public class ReservationItem
{
    public int Id { get; set; }
    public int ReservationId { get; set; }
    public int MenuItemId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }

    // მხოლოდ წაკითხვისთვის — admin გვერდზე კერძის სახელის საჩვენებლად
    public string MenuItemName { get; set; } = string.Empty;
}