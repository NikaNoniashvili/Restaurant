using System.Data;
using Restaurant.Models;
using Restaurant.Repository.Interface;
using Restaurant.Services;

namespace Restaurant.Repository;

public class SiteTextRepository : ISiteTextRepository
{
    private readonly DatabaseService _databaseService;

    public SiteTextRepository(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public List<SiteText> GetAll()
    {
        var items = new List<SiteText>();

        using (var connection = _databaseService.GetConnection())
        {
            connection.Open();

            string query = "SELECT Id, KeyName, ValueKa, ValueEn, ValueRu FROM SiteText";

            using (var command = new Microsoft.Data.SqlClient.SqlCommand(query, (Microsoft.Data.SqlClient.SqlConnection)connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    items.Add(new SiteText
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        KeyName = reader["KeyName"].ToString() ?? string.Empty,
                        ValueKa = reader["ValueKa"].ToString() ?? string.Empty,
                        ValueEn = reader["ValueEn"].ToString() ?? string.Empty,
                        ValueRu = reader["ValueRu"].ToString() ?? string.Empty
                    });
                }
            }
        }

        return items;
    }
}