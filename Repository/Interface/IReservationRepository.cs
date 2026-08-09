using Restaurant.Models;

namespace Restaurant.Repository.Interface;

public interface IReservationRepository
{
    int Create(Reservation reservation, List<ReservationItem> items);
    List<Reservation> GetAll();
    Reservation? GetById(int id);
}