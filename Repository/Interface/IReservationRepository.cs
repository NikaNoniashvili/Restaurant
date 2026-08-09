using Restaurant.Models;

namespace Restaurant.Repository.Interface;

public interface IReservationRepository
{
    int Create(Reservation reservation, List<ReservationItem> items);
    List<Reservation> GetAll();
    Reservation? GetById(int id);

    List<Reservation> GetActive();
    List<Reservation> GetCompleted();
    List<Reservation> GetCancelled();

    void MarkDepositPaid(int reservationId);
    void MarkFinalPaid(int reservationId);
    void CancelReservation(int reservationId);
}