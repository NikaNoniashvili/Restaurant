using Restaurant.Models;

namespace Restaurant.Repository.Interface;

public interface ITableRepository
{
    List<RestaurantTable> GetAll();
    List<RestaurantTable> GetActive();
    RestaurantTable? GetById(int id);
    int Create(RestaurantTable table);
    void Update(RestaurantTable table);
    void SetActiveStatus(int id, bool isActive);

    // ✅ დაკავებული მაგიდების შემოწმება კონკრეტულ თარიღზე
    List<int> GetOccupiedTableIds(DateTime date);

    // ✅ თავისუფალი მაგიდები კონკრეტულ თარიღზე, ტევადობის მიხედვით დალაგებული
    List<RestaurantTable> GetAvailableTables(DateTime date, int guestCount);

    // ✅ ახალი — ზუსტი დრო+ხანგრძლივობის გათვალისწინებით
    List<RestaurantTable> GetAvailableTablesForSlot(DateTime date, TimeSpan time, int guestCount, int durationHours, int holdMinutes);
}