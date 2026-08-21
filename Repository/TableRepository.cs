using Microsoft.Data.SqlClient;
using Restaurant.Models;
using Restaurant.Repository.Interface;
using Restaurant.Services;

namespace Restaurant.Repository;

public class TableRepository : ITableRepository
{
    private readonly DatabaseService _databaseService;

    public TableRepository(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    // ==========================================
    // GetAll — ყველა მაგიდა (Admin Table Management-ისთვის)
    // ==========================================
    public List<RestaurantTable> GetAll()
    {
        var tables = new List<RestaurantTable>();

        using (var connection = (SqlConnection)_databaseService.GetConnection())
        {
            connection.Open();

            string query = "SELECT Id, TableNumber, Capacity, IsActive FROM [Table] ORDER BY TableNumber ASC";

            using (var cmd = new SqlCommand(query, connection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    tables.Add(MapTable(reader));
                }
            }
        }

        return tables;
    }

    // ==========================================
    // GetActive — მხოლოდ აქტიური მაგიდები
    // ==========================================
    public List<RestaurantTable> GetActive()
    {
        var tables = new List<RestaurantTable>();

        using (var connection = (SqlConnection)_databaseService.GetConnection())
        {
            connection.Open();

            string query = "SELECT Id, TableNumber, Capacity, IsActive FROM [Table] WHERE IsActive = 1 ORDER BY TableNumber ASC";

            using (var cmd = new SqlCommand(query, connection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    tables.Add(MapTable(reader));
                }
            }
        }

        return tables;
    }

    // ==========================================
    // GetById
    // ==========================================
    public RestaurantTable? GetById(int id)
    {
        using (var connection = (SqlConnection)_databaseService.GetConnection())
        {
            connection.Open();

            string query = "SELECT Id, TableNumber, Capacity, IsActive FROM [Table] WHERE Id = @Id";

            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@Id", id);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return MapTable(reader);
                    }
                }
            }
        }

        return null;
    }

    // ==========================================
    // Create — ახალი მაგიდის დამატება
    // ==========================================
    public int Create(RestaurantTable table)
    {
        using (var connection = (SqlConnection)_databaseService.GetConnection())
        {
            connection.Open();

            string query = @"
                INSERT INTO [Table] (TableNumber, Capacity, IsActive)
                OUTPUT INSERTED.Id
                VALUES (@TableNumber, @Capacity, @IsActive)";

            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@TableNumber", table.TableNumber);
                cmd.Parameters.AddWithValue("@Capacity", table.Capacity);
                cmd.Parameters.AddWithValue("@IsActive", table.IsActive);

                return (int)cmd.ExecuteScalar();
            }
        }
    }

    // ==========================================
    // Update — TableNumber/Capacity-ის შეცვლა
    // ==========================================
    public void Update(RestaurantTable table)
    {
        using (var connection = (SqlConnection)_databaseService.GetConnection())
        {
            connection.Open();

            string query = @"
                UPDATE [Table]
                SET TableNumber = @TableNumber, Capacity = @Capacity
                WHERE Id = @Id";

            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@Id", table.Id);
                cmd.Parameters.AddWithValue("@TableNumber", table.TableNumber);
                cmd.Parameters.AddWithValue("@Capacity", table.Capacity);
                cmd.ExecuteNonQuery();
            }
        }
    }

    // ==========================================
    // SetActiveStatus — აქტიური/არააქტიური გადართვა
    // ==========================================
    public void SetActiveStatus(int id, bool isActive)
    {
        using (var connection = (SqlConnection)_databaseService.GetConnection())
        {
            connection.Open();

            string query = "UPDATE [Table] SET IsActive = @IsActive WHERE Id = @Id";

            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@IsActive", isActive);
                cmd.ExecuteNonQuery();
            }
        }
    }

    // ==========================================
    // GetOccupiedTableIds — რომელი მაგიდებია დაკავებული კონკრეტულ თარიღზე
    // "დაკავებული" = Reservation, რომელიც ამ თარიღზეა და სტატუსი Confirmed ან InProgress
    // ==========================================
    public List<int> GetOccupiedTableIds(DateTime date)
    {
        var occupiedIds = new List<int>();

        using (var connection = (SqlConnection)_databaseService.GetConnection())
        {
            connection.Open();

            string query = @"
                SELECT DISTINCT TableId
                FROM Reservation
                WHERE TableId IS NOT NULL
                  AND ReservationDate = @Date
                  AND ReservationStatus IN ('Confirmed', 'InProgress')";

            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@Date", date.Date);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        occupiedIds.Add(Convert.ToInt32(reader["TableId"]));
                    }
                }
            }
        }

        return occupiedIds;
    }

    // ==========================================
    // GetAvailableTables — თავისუფალი მაგიდები, ტევადობით დალაგებული
    // (მცირე ტევადობა უფრო წინ — რომ 2 კაცზე 2-ადგილიანი შეთავაზდეს, არა 8-ადგილიანი)
    // ==========================================
    public List<RestaurantTable> GetAvailableTables(DateTime date, int guestCount)
    {
        var occupiedIds = GetOccupiedTableIds(date);
        var activeTables = GetActive();

        return activeTables
            .Where(t => !occupiedIds.Contains(t.Id) && t.Capacity >= guestCount)
            .OrderBy(t => t.Capacity)
            .ToList();
    }

    // ==========================================
    // Helper
    // ==========================================
    private RestaurantTable MapTable(SqlDataReader reader)
    {
        return new RestaurantTable
        {
            Id = Convert.ToInt32(reader["Id"]),
            TableNumber = Convert.ToInt32(reader["TableNumber"]),
            Capacity = Convert.ToInt32(reader["Capacity"]),
            IsActive = Convert.ToBoolean(reader["IsActive"])
        };
    }

    // ==========================================
    // ✅ GetAvailableTablesForSlot — ზუსტი overlap-შემოწმება
    // ==========================================
    public List<RestaurantTable> GetAvailableTablesForSlot(DateTime date, TimeSpan time, int guestCount, int durationHours, int holdMinutes)
    {
        var newStart = date.Date + time;
        var newEnd = newStart.AddHours(durationHours);

        var tables = new List<RestaurantTable>();

        using (var connection = (SqlConnection)_databaseService.GetConnection())
        {
            connection.Open();

            string query = @"
            SELECT t.Id, t.TableNumber, t.Capacity, t.IsActive
            FROM [Table] t
            WHERE t.IsActive = 1 AND t.Capacity >= @GuestCount
              AND t.Id NOT IN (
                  SELECT r.TableId
                  FROM Reservation r
                  WHERE r.TableId IS NOT NULL
                    AND (
                        r.ReservationStatus IN ('Confirmed', 'InProgress')
                        OR (r.ReservationStatus = 'Pending' AND r.CreatedAt >= DATEADD(MINUTE, -@HoldMinutes, GETDATE()))
                    )
                    AND (CAST(r.ReservationDate AS DATETIME) + CAST(r.ReservationTime AS DATETIME)) < @NewEnd
                    AND r.EstimatedEndDateTime > @NewStart
              )
            ORDER BY t.Capacity ASC";

            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@GuestCount", guestCount);
                cmd.Parameters.AddWithValue("@HoldMinutes", holdMinutes);
                cmd.Parameters.AddWithValue("@NewStart", newStart);
                cmd.Parameters.AddWithValue("@NewEnd", newEnd);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tables.Add(MapTable(reader));
                    }
                }
            }
        }

        return tables;
    }
}