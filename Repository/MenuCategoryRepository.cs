using Microsoft.Data.SqlClient;
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

    // ==========================================
    // GetAll — ყველა კატეგორია (Admin-ისთვის, აქტიური + არააქტიური)
    // ==========================================
    public List<MenuCategory> GetAll()
    {
        var items = new List<MenuCategory>();

        using (var connection = (SqlConnection)_databaseService.GetConnection())
        {
            connection.Open();

            string query = "SELECT Id, NameKa, NameEn, NameRu, SortOrder, IsActive FROM MenuCategory ORDER BY SortOrder";

            using (var cmd = new SqlCommand(query, connection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    items.Add(MapCategory(reader));
                }
            }
        }

        return items;
    }

    // ==========================================
    // GetActive — მხოლოდ საიტისთვის (IsActive = true)
    // ==========================================
    public List<MenuCategory> GetActive()
    {
        var items = new List<MenuCategory>();

        using (var connection = (SqlConnection)_databaseService.GetConnection())
        {
            connection.Open();

            string query = "SELECT Id, NameKa, NameEn, NameRu, SortOrder, IsActive FROM MenuCategory WHERE IsActive = 1 ORDER BY SortOrder";

            using (var cmd = new SqlCommand(query, connection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    items.Add(MapCategory(reader));
                }
            }
        }

        return items;
    }

    // ==========================================
    // GetById
    // ==========================================
    public MenuCategory? GetById(int id)
    {
        using (var connection = (SqlConnection)_databaseService.GetConnection())
        {
            connection.Open();

            string query = "SELECT Id, NameKa, NameEn, NameRu, SortOrder, IsActive FROM MenuCategory WHERE Id = @Id";

            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@Id", id);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return MapCategory(reader);
                    }
                }
            }
        }

        return null;
    }

    // ==========================================
    // Create
    // ==========================================
    public int Create(MenuCategory category)
    {
        using (var connection = (SqlConnection)_databaseService.GetConnection())
        {
            connection.Open();

            string query = @"
                INSERT INTO MenuCategory (NameKa, NameEn, NameRu, SortOrder, IsActive)
                OUTPUT INSERTED.Id
                VALUES (@NameKa, @NameEn, @NameRu, @SortOrder, @IsActive)";

            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@NameKa", category.NameKa);
                cmd.Parameters.AddWithValue("@NameEn", category.NameEn);
                cmd.Parameters.AddWithValue("@NameRu", category.NameRu);
                cmd.Parameters.AddWithValue("@SortOrder", category.SortOrder);
                cmd.Parameters.AddWithValue("@IsActive", true);

                return (int)cmd.ExecuteScalar();
            }
        }
    }

    // ==========================================
    // Update
    // ==========================================
    public void Update(MenuCategory category)
    {
        using (var connection = (SqlConnection)_databaseService.GetConnection())
        {
            connection.Open();

            string query = @"
                UPDATE MenuCategory
                SET NameKa = @NameKa, NameEn = @NameEn, NameRu = @NameRu, SortOrder = @SortOrder
                WHERE Id = @Id";

            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@Id", category.Id);
                cmd.Parameters.AddWithValue("@NameKa", category.NameKa);
                cmd.Parameters.AddWithValue("@NameEn", category.NameEn);
                cmd.Parameters.AddWithValue("@NameRu", category.NameRu);
                cmd.Parameters.AddWithValue("@SortOrder", category.SortOrder);
                cmd.ExecuteNonQuery();
            }
        }
    }

    // ==========================================
    // SetActiveStatus — Soft Delete / აღდგენა
    // ==========================================
    public void SetActiveStatus(int id, bool isActive)
    {
        using (var connection = (SqlConnection)_databaseService.GetConnection())
        {
            connection.Open();

            string query = "UPDATE MenuCategory SET IsActive = @IsActive WHERE Id = @Id";

            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@IsActive", isActive);
                cmd.ExecuteNonQuery();
            }
        }
    }

    // ==========================================
    // Helper
    // ==========================================
    private MenuCategory MapCategory(SqlDataReader reader)
    {
        return new MenuCategory
        {
            Id = Convert.ToInt32(reader["Id"]),
            NameKa = reader["NameKa"].ToString() ?? string.Empty,
            NameEn = reader["NameEn"].ToString() ?? string.Empty,
            NameRu = reader["NameRu"].ToString() ?? string.Empty,
            SortOrder = Convert.ToInt32(reader["SortOrder"]),
            IsActive = Convert.ToBoolean(reader["IsActive"])
        };
    }
}