using Restaurant.Models;

namespace Restaurant.Repository.Interface;

public interface IMenuCategoryRepository
{
    List<MenuCategory> GetAll();
}