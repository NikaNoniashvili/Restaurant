using Restaurant.Models;
using Restaurant.Repository.Interface;
using Restaurant.Services;

namespace Restaurant.Repository;

public class RestaurantInfoRepository : IRestaurantInfoRepository
{
    private readonly DatabaseService _databaseService;

    public RestaurantInfoRepository(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public RestaurantInfo? Get()
    {
        using (var connection = _databaseService.GetConnection())
        {
            connection.Open();

            string query = @"SELECT Id, AddressKa, AddressEn, AddressRu, Phone, Email, 
                              WorkingHoursKa, WorkingHoursEn, WorkingHoursRu, 
                              FacebookUrl, InstagramUrl, MapEmbedUrl 
                              FROM RestaurantInfo WHERE Id = 1";

            using (var command = new Microsoft.Data.SqlClient.SqlCommand(query, (Microsoft.Data.SqlClient.SqlConnection)connection))
            using (var reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    return new RestaurantInfo
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        AddressKa = reader["AddressKa"].ToString() ?? string.Empty,
                        AddressEn = reader["AddressEn"].ToString() ?? string.Empty,
                        AddressRu = reader["AddressRu"].ToString() ?? string.Empty,
                        Phone = reader["Phone"].ToString() ?? string.Empty,
                        Email = reader["Email"].ToString() ?? string.Empty,
                        WorkingHoursKa = reader["WorkingHoursKa"].ToString() ?? string.Empty,
                        WorkingHoursEn = reader["WorkingHoursEn"].ToString() ?? string.Empty,
                        WorkingHoursRu = reader["WorkingHoursRu"].ToString() ?? string.Empty,
                        FacebookUrl = reader["FacebookUrl"] == DBNull.Value ? null : reader["FacebookUrl"].ToString(),
                        InstagramUrl = reader["InstagramUrl"] == DBNull.Value ? null : reader["InstagramUrl"].ToString(),
                        MapEmbedUrl = reader["MapEmbedUrl"] == DBNull.Value ? null : reader["MapEmbedUrl"].ToString()
                    };
                }
            }
        }

        return null;
    }
}