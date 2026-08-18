using Microsoft.Data.SqlClient;
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

    // ==========================================
    // GetAll — ყველა კერძი (ისტორიული/Reservation-ისთვის, IsActive-ის მიუხედავად)
    // ==========================================
    public List<MenuItem> GetAll()
    {
        var items = new List<MenuItem>();

        using (var connection = (SqlConnection)_databaseService.GetConnection())
        {
            connection.Open();

            string query = @"SELECT Id, CategoryId, NameKa, NameEn, NameRu, 
                              DescriptionKa, DescriptionEn, DescriptionRu, 
                              Price, ImageUrl, SortOrder, IsActive 
                              FROM MenuItem ORDER BY CategoryId, SortOrder";

            using (var cmd = new SqlCommand(query, connection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    items.Add(MapItem(reader));
                }
            }
        }

        return items;
    }

    // ==========================================
    // GetActive — მხოლოდ საიტისთვის (IsActive = true)
    // ==========================================
    public List<MenuItem> GetActive()
    {
        var items = new List<MenuItem>();

        using (var connection = (SqlConnection)_databaseService.GetConnection())
        {
            connection.Open();

            string query = @"SELECT Id, CategoryId, NameKa, NameEn, NameRu, 
                              DescriptionKa, DescriptionEn, DescriptionRu, 
                              Price, ImageUrl, SortOrder, IsActive 
                              FROM MenuItem WHERE IsActive = 1 ORDER BY CategoryId, SortOrder";

            using (var cmd = new SqlCommand(query, connection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    items.Add(MapItem(reader));
                }
            }
        }

        return items;
    }

    // ==========================================
    // GetAllForAdmin — Admin სია, JOIN კატეგორიის სახელით, search + filter
    // ==========================================
    public List<MenuItem> GetAllForAdmin(string? searchTerm = null, int? categoryId = null)
    {
        var items = new List<MenuItem>();

        using (var connection = (SqlConnection)_databaseService.GetConnection())
        {
            connection.Open();

            string query = @"
                SELECT mi.Id, mi.CategoryId, mi.NameKa, mi.NameEn, mi.NameRu,
                       mi.DescriptionKa, mi.DescriptionEn, mi.DescriptionRu,
                       mi.Price, mi.ImageUrl, mi.SortOrder, mi.IsActive,
                       mc.NameKa AS CategoryName
                FROM MenuItem mi
                INNER JOIN MenuCategory mc ON mi.CategoryId = mc.Id
                WHERE 1 = 1";

            if (categoryId.HasValue)
            {
                query += " AND mi.CategoryId = @CategoryId";
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query += " AND mi.NameKa LIKE @SearchTerm";
            }

            query += " ORDER BY mc.SortOrder, mi.SortOrder";

            using (var cmd = new SqlCommand(query, connection))
            {
                if (categoryId.HasValue)
                {
                    cmd.Parameters.AddWithValue("@CategoryId", categoryId.Value);
                }

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    cmd.Parameters.AddWithValue("@SearchTerm", "%" + searchTerm.Trim() + "%");
                }

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var item = MapItem(reader);
                        item.CategoryName = reader["CategoryName"].ToString();
                        items.Add(item);
                    }
                }
            }
        }

        return items;
    }

    // ==========================================
    // GetById
    // ==========================================
    public MenuItem? GetById(int id)
    {
        using (var connection = (SqlConnection)_databaseService.GetConnection())
        {
            connection.Open();

            string query = @"SELECT Id, CategoryId, NameKa, NameEn, NameRu, 
                              DescriptionKa, DescriptionEn, DescriptionRu, 
                              Price, ImageUrl, SortOrder, IsActive 
                              FROM MenuItem WHERE Id = @Id";

            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@Id", id);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return MapItem(reader);
                    }
                }
            }
        }

        return null;
    }

    // ==========================================
    // Create
    // ==========================================
    public int Create(MenuItem item)
    {
        using (var connection = (SqlConnection)_databaseService.GetConnection())
        {
            connection.Open();

            string query = @"
                INSERT INTO MenuItem 
                    (CategoryId, NameKa, NameEn, NameRu, DescriptionKa, DescriptionEn, DescriptionRu, 
                     Price, ImageUrl, SortOrder, IsActive)
                OUTPUT INSERTED.Id
                VALUES 
                    (@CategoryId, @NameKa, @NameEn, @NameRu, @DescriptionKa, @DescriptionEn, @DescriptionRu,
                     @Price, @ImageUrl, @SortOrder, @IsActive)";

            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@CategoryId", item.CategoryId);
                cmd.Parameters.AddWithValue("@NameKa", item.NameKa);
                cmd.Parameters.AddWithValue("@NameEn", item.NameEn);
                cmd.Parameters.AddWithValue("@NameRu", item.NameRu);
                cmd.Parameters.AddWithValue("@DescriptionKa", item.DescriptionKa);
                cmd.Parameters.AddWithValue("@DescriptionEn", item.DescriptionEn);
                cmd.Parameters.AddWithValue("@DescriptionRu", item.DescriptionRu);
                cmd.Parameters.AddWithValue("@Price", item.Price);
                cmd.Parameters.AddWithValue("@ImageUrl", (object?)item.ImageUrl ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@SortOrder", item.SortOrder);
                cmd.Parameters.AddWithValue("@IsActive", true);

                return (int)cmd.ExecuteScalar();
            }
        }
    }

    // ==========================================
    // Update
    // ==========================================
    public void Update(MenuItem item)
    {
        using (var connection = (SqlConnection)_databaseService.GetConnection())
        {
            connection.Open();

            string query = @"
                UPDATE MenuItem
                SET CategoryId = @CategoryId, NameKa = @NameKa, NameEn = @NameEn, NameRu = @NameRu,
                    DescriptionKa = @DescriptionKa, DescriptionEn = @DescriptionEn, DescriptionRu = @DescriptionRu,
                    Price = @Price, ImageUrl = @ImageUrl, SortOrder = @SortOrder
                WHERE Id = @Id";

            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@Id", item.Id);
                cmd.Parameters.AddWithValue("@CategoryId", item.CategoryId);
                cmd.Parameters.AddWithValue("@NameKa", item.NameKa);
                cmd.Parameters.AddWithValue("@NameEn", item.NameEn);
                cmd.Parameters.AddWithValue("@NameRu", item.NameRu);
                cmd.Parameters.AddWithValue("@DescriptionKa", item.DescriptionKa);
                cmd.Parameters.AddWithValue("@DescriptionEn", item.DescriptionEn);
                cmd.Parameters.AddWithValue("@DescriptionRu", item.DescriptionRu);
                cmd.Parameters.AddWithValue("@Price", item.Price);
                cmd.Parameters.AddWithValue("@ImageUrl", (object?)item.ImageUrl ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@SortOrder", item.SortOrder);
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

            string query = "UPDATE MenuItem SET IsActive = @IsActive WHERE Id = @Id";

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
    private MenuItem MapItem(SqlDataReader reader)
    {
        return new MenuItem
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
            SortOrder = Convert.ToInt32(reader["SortOrder"]),
            IsActive = Convert.ToBoolean(reader["IsActive"])
        };
    }
}