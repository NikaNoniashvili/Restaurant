using Restaurant.Models;

namespace Restaurant.Repository.Interface;

public interface IMenuItemRepository
{
    List<MenuItem> GetAll();
    List<MenuItem> GetActive();
    List<MenuItem> GetAllForAdmin(string? searchTerm = null, int? categoryId = null);
    MenuItem? GetById(int id);
    int Create(MenuItem item);
    void Update(MenuItem item);
    void SetActiveStatus(int id, bool isActive);
}