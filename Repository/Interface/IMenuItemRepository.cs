using Restaurant.Models;

namespace Restaurant.Repository.Interface;

public interface IMenuItemRepository
{
    List<MenuItem> GetAll();
}