using Microsoft.AspNetCore.Mvc;
using Restaurant.Repository.Interface;
using Restaurant.Models;

namespace Restaurant.Controllers;

// ==========================================
// Request DTO — მომხმარებლისგან მხოლოდ ეს მოდის (ფასის გარეშე!)
// ==========================================
public class ReservationItemRequest
{
    public int MenuItemId { get; set; }
    public int Quantity { get; set; }
}

public class ReservationRequest
{
    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime ReservationDate { get; set; }
    public string ReservationTime { get; set; } = string.Empty; // "18:30" ფორმატი
    public int GuestCount { get; set; }
    public List<ReservationItemRequest> Items { get; set; } = new();
}

[Route("api/[controller]")]
[ApiController]
public class ReservationController : ControllerBase
{
    private readonly IReservationRepository _reservationRepository;
    private readonly IMenuItemRepository _menuItemRepository;

    public ReservationController(
        IReservationRepository reservationRepository,
        IMenuItemRepository menuItemRepository)
    {
        _reservationRepository = reservationRepository;
        _menuItemRepository = menuItemRepository;
    }

    // ==========================================
    // POST — ახალი ჯავშნის შექმნა
    // ==========================================
    [HttpPost]
    public IActionResult Create([FromBody] ReservationRequest request)
    {
        // ვალიდაცია — საბაზისო შემოწმებები
        if (string.IsNullOrWhiteSpace(request.CustomerName))
            return BadRequest(new { error = "სახელი აუცილებელია" });

        if (string.IsNullOrWhiteSpace(request.Phone))
            return BadRequest(new { error = "ტელეფონი აუცილებელია" });

        if (request.GuestCount <= 0)
            return BadRequest(new { error = "სტუმრების რაოდენობა უნდა იყოს 1 ან მეტი" });

        if (request.Items == null || request.Items.Count == 0)
            return BadRequest(new { error = "მინიმუმ ერთი კერძი უნდა აირჩიოთ" });

        if (!TimeSpan.TryParse(request.ReservationTime, out var parsedTime))
            return BadRequest(new { error = "დროის ფორმატი არასწორია" });

        // ⚠️ ყველა კერძის რეალური ფასი ბაზიდან წამოვიღოთ — client-ის ფასს არასდროს ვენდობით
        var allMenuItems = _menuItemRepository.GetAll();
        var reservationItems = new List<ReservationItem>();
        decimal totalAmount = 0;

        foreach (var reqItem in request.Items)
        {
            if (reqItem.Quantity <= 0)
                return BadRequest(new { error = "რაოდენობა უნდა იყოს 1 ან მეტი" });

            var menuItem = allMenuItems.FirstOrDefault(m => m.Id == reqItem.MenuItemId);
            if (menuItem == null)
                return BadRequest(new { error = $"კერძი ID={reqItem.MenuItemId} ვერ მოიძებნა" });

            // ✅ ფასი მხოლოდ სერვერზე, ბაზიდან
            decimal unitPrice = menuItem.Price;
            decimal lineTotal = unitPrice * reqItem.Quantity;
            totalAmount += lineTotal;

            reservationItems.Add(new ReservationItem
            {
                MenuItemId = reqItem.MenuItemId,
                Quantity = reqItem.Quantity,
                UnitPrice = unitPrice,
                LineTotal = lineTotal
            });
        }

        decimal prepaidAmount = Math.Round(totalAmount * 0.5m, 2);
        decimal remainingAmount = totalAmount - prepaidAmount;

        var reservation = new Reservation
        {
            CustomerName = request.CustomerName.Trim(),
            Phone = request.Phone.Trim(),
            ReservationDate = request.ReservationDate.Date,
            ReservationTime = parsedTime,
            GuestCount = request.GuestCount,
            TotalAmount = totalAmount,
            PrepaidAmount = prepaidAmount,
            RemainingAmount = remainingAmount,
            ReservationStatus = "Pending",
            PaymentStatus = "Unpaid"
        };

        int newId = _reservationRepository.Create(reservation, reservationItems);

        return Ok(new
        {
            reservationId = newId,
            totalAmount,
            prepaidAmount,
            remainingAmount
        });
    }

    // ==========================================
    // GET — ყველა ჯავშანი (Admin გვერდისთვის)
    // ==========================================
    [HttpGet]
    public IActionResult GetAll()
    {
        var reservations = _reservationRepository.GetAll();

        var result = reservations.Select(r => new
        {
            r.Id,
            r.CustomerName,
            r.Phone,
            ReservationDate = r.ReservationDate.ToString("yyyy-MM-dd"),
            ReservationTime = r.ReservationTime.ToString(@"hh\:mm"),
            r.GuestCount,
            r.TotalAmount,
            r.PrepaidAmount,
            r.RemainingAmount,
            r.ReservationStatus,
            r.PaymentStatus,
            CreatedAt = r.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
            Items = r.Items.Select(i => new
            {
                i.MenuItemName,
                i.Quantity,
                i.UnitPrice,
                i.LineTotal
            })
        });

        return Ok(result);
    }
}