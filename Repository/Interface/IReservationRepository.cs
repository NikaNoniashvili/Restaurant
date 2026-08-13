using Restaurant.Models;

namespace Restaurant.Repository.Interface;

public interface IReservationRepository
{
    int Create(Reservation reservation, List<ReservationItem> items);
    List<Reservation> GetAll();
    Reservation? GetById(int id);

    List<Reservation> GetActive(string? searchTerm = null);
    List<Reservation> GetCompleted(string? searchTerm = null);
    List<Reservation> GetCancelled(string? searchTerm = null);
    List<MonthlyStatRow> GetMonthlyStatistics(int year);

    void MarkDepositPaid(int reservationId);
    void MarkFinalPaid(int reservationId);
    void CancelReservation(int reservationId);
}