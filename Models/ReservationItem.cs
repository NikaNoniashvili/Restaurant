namespace Restaurant.Models;

public class ReservationItem
{
    public int Id { get; set; }
    public int ReservationId { get; set; }
    public int MenuItemId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }

    // ✅ ახალი — შეკვეთის შექმნის მომენტში დაფიქსირებული სახელი
    public string NameSnapshot { get; set; } = string.Empty;

    // ⚠️ ეს property-ს დანიშნულება იცვლება: ის აღარ არის ცოცხალი JOIN-ის შედეგი,
    // არამედ უბრალოდ NameSnapshot-ის ასლი — Admin View-ებში იმავე სახელით გამოვიყენოთ
    public string MenuItemName { get; set; } = string.Empty;
}