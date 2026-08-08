using Restaurant.Models;
using Restaurant.Repository.Interface;
using Restaurant.Services;

namespace Restaurant.Repository;

public class GalleryPhotoRepository : IGalleryPhotoRepository
{
    private readonly DatabaseService _databaseService;

    public GalleryPhotoRepository(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public List<GalleryPhoto> GetAll()
    {
        var items = new List<GalleryPhoto>();

        using (var connection = _databaseService.GetConnection())
        {
            connection.Open();

            string query = "SELECT Id, TitleKa, TitleEn, TitleRu, ImageUrl, SortOrder FROM GalleryPhoto ORDER BY SortOrder";

            using (var command = new Microsoft.Data.SqlClient.SqlCommand(query, (Microsoft.Data.SqlClient.SqlConnection)connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    items.Add(new GalleryPhoto
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        TitleKa = reader["TitleKa"].ToString() ?? string.Empty,
                        TitleEn = reader["TitleEn"].ToString() ?? string.Empty,
                        TitleRu = reader["TitleRu"].ToString() ?? string.Empty,
                        ImageUrl = reader["ImageUrl"].ToString() ?? string.Empty,
                        SortOrder = Convert.ToInt32(reader["SortOrder"])
                    });
                }
            }
        }

        return items;
    }
}