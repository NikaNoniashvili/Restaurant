using Restaurant.Models;

namespace Restaurant.Repository.Interface;

public interface IReservationRepository
{
    int Create(Reservation reservation, List<ReservationItem> items);
    List<Reservation> GetAll();
    Reservation? GetById(int id);
    List<Reservation> GetByDate(DateTime date);

    List<Reservation> GetActive(string? searchTerm = null);
    List<Reservation> GetCompleted(string? searchTerm = null);
    List<Reservation> GetCancelled(string? searchTerm = null);

    void MarkDepositPaid(int reservationId);
    void MarkFinalPaid(int reservationId);
    void CancelReservation(int reservationId);

    void AssignTable(int reservationId, int tableId);
    void StartReservation(int reservationId);

    List<MonthlyStatRow> GetMonthlyStatistics(int year);
}