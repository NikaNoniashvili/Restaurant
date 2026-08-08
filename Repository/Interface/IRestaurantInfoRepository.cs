using Restaurant.Models;

namespace Restaurant.Repository.Interface;

public interface IRestaurantInfoRepository
{
    RestaurantInfo? Get();
}