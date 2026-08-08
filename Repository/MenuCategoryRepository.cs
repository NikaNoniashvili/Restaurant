using Restaurant.Models;
using Restaurant.Repository.Interface;
using Restaurant.Services;

namespace Restaurant.Repository;

public class MenuCategoryRepository : IMenuCategoryRepository
{
    private readonly DatabaseService _databaseService;

    public MenuCategoryRepository(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public List<MenuCategory> GetAll()
    {
        var items = new List<MenuCategory>();

        using (var connection = _databaseService.GetConnection())
        {
            connection.Open();

            string query = "SELECT Id, NameKa, NameEn, NameRu, SortOrder FROM MenuCategory ORDER BY SortOrder";

            using (var command = new Microsoft.Data.SqlClient.SqlCommand(query, (Microsoft.Data.SqlClient.SqlConnection)connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    items.Add(new MenuCategory
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        NameKa = reader["NameKa"].ToString() ?? string.Empty,
                        NameEn = reader["NameEn"].ToString() ?? string.Empty,
                        NameRu = reader["NameRu"].ToString() ?? string.Empty,
                        SortOrder = Convert.ToInt32(reader["SortOrder"])
                    });
                }
            }
        }

        return items;
    }
}