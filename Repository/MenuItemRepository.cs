using Restaurant.Models;
using Restaurant.Repository.Interface;
using Restaurant.Services;

namespace Restaurant.Repository;

public class MenuItemRepository : IMenuItemRepository
{
    private readonly DatabaseService _databaseService;

    public MenuItemRepository(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public List<MenuItem> GetAll()
    {
        var items = new List<MenuItem>();

        using (var connection = _databaseService.GetConnection())
        {
            connection.Open();

            string query = @"SELECT Id, CategoryId, NameKa, NameEn, NameRu, 
                              DescriptionKa, DescriptionEn, DescriptionRu, 
                              Price, ImageUrl, SortOrder 
                              FROM MenuItem ORDER BY CategoryId, SortOrder";

            using (var command = new Microsoft.Data.SqlClient.SqlCommand(query, (Microsoft.Data.SqlClient.SqlConnection)connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    items.Add(new MenuItem
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        CategoryId = Convert.ToInt32(reader["CategoryId"]),
                        NameKa = reader["NameKa"].ToString() ?? string.Empty,
                        NameEn = reader["NameEn"].ToString() ?? string.Empty,
                        NameRu = reader["NameRu"].ToString() ?? string.Empty,
                        DescriptionKa = reader["DescriptionKa"].ToString() ?? string.Empty,
                        DescriptionEn = reader["DescriptionEn"].ToString() ?? string.Empty,
                        DescriptionRu = reader["DescriptionRu"].ToString() ?? string.Empty,
                        Price = Convert.ToDecimal(reader["Price"]),
                        ImageUrl = reader["ImageUrl"] == DBNull.Value ? null : reader["ImageUrl"].ToString(),
                        SortOrder = Convert.ToInt32(reader["SortOrder"])
                    });
                }
            }
        }

        return items;
    }
}