using Restaurant.Models;

namespace Restaurant.Repository.Interface;

public interface IMenuCategoryRepository
{
    List<MenuCategory> GetAll();
    List<MenuCategory> GetActive();
    MenuCategory? GetById(int id);
    int Create(MenuCategory category);
    void Update(MenuCategory category);
    void SetActiveStatus(int id, bool isActive);
}