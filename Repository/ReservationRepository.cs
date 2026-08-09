using Microsoft.Data.SqlClient;
using Restaurant.Models;
using Restaurant.Repository.Interface;
using Restaurant.Services;

namespace Restaurant.Repository;

public class ReservationRepository : IReservationRepository
{
    private readonly DatabaseService _databaseService;

    public ReservationRepository(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    // ==========================================
    // Create — ჯავშანი + მისი items ერთ ტრანზაქციაში
    // ==========================================
    public int Create(Reservation reservation, List<ReservationItem> items)
    {
        using (var connection = (SqlConnection)_databaseService.GetConnection())
        {
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    int reservationId;

                    // 1. Reservation ჩაწერა
                    string insertReservation = @"
                        INSERT INTO Reservation 
                            (CustomerName, Phone, ReservationDate, ReservationTime, GuestCount,
                             TotalAmount, PrepaidAmount, RemainingAmount, ReservationStatus, PaymentStatus, CreatedAt)
                        OUTPUT INSERTED.Id
                        VALUES 
                            (@CustomerName, @Phone, @ReservationDate, @ReservationTime, @GuestCount,
                             @TotalAmount, @PrepaidAmount, @RemainingAmount, @ReservationStatus, @PaymentStatus, @CreatedAt)";

                    using (var cmd = new SqlCommand(insertReservation, connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@CustomerName", reservation.CustomerName);
                        cmd.Parameters.AddWithValue("@Phone", reservation.Phone);
                        cmd.Parameters.AddWithValue("@ReservationDate", reservation.ReservationDate);
                        cmd.Parameters.AddWithValue("@ReservationTime", reservation.ReservationTime);
                        cmd.Parameters.AddWithValue("@GuestCount", reservation.GuestCount);
                        cmd.Parameters.AddWithValue("@TotalAmount", reservation.TotalAmount);
                        cmd.Parameters.AddWithValue("@PrepaidAmount", reservation.PrepaidAmount);
                        cmd.Parameters.AddWithValue("@RemainingAmount", reservation.RemainingAmount);
                        cmd.Parameters.AddWithValue("@ReservationStatus", reservation.ReservationStatus);
                        cmd.Parameters.AddWithValue("@PaymentStatus", reservation.PaymentStatus);
                        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

                        reservationId = (int)cmd.ExecuteScalar();
                    }

                    // 2. ReservationItem-ების ჩაწერა
                    string insertItem = @"
                        INSERT INTO ReservationItem (ReservationId, MenuItemId, Quantity, UnitPrice, LineTotal)
                        VALUES (@ReservationId, @MenuItemId, @Quantity, @UnitPrice, @LineTotal)";

                    foreach (var item in items)
                    {
                        using (var cmd = new SqlCommand(insertItem, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@ReservationId", reservationId);
                            cmd.Parameters.AddWithValue("@MenuItemId", item.MenuItemId);
                            cmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                            cmd.Parameters.AddWithValue("@UnitPrice", item.UnitPrice);
                            cmd.Parameters.AddWithValue("@LineTotal", item.LineTotal);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                    return reservationId;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }

    // ==========================================
    // GetAll — Admin გვერდისთვის, ყველა ჯავშანი + items
    // ==========================================
    public List<Reservation> GetAll()
    {
        var reservations = new List<Reservation>();

        using (var connection = (SqlConnection)_databaseService.GetConnection())
        {
            connection.Open();

            string query = @"
                SELECT Id, CustomerName, Phone, ReservationDate, ReservationTime, GuestCount,
                       TotalAmount, PrepaidAmount, RemainingAmount, ReservationStatus, PaymentStatus, CreatedAt
                FROM Reservation
                ORDER BY CreatedAt DESC";

            using (var cmd = new SqlCommand(query, connection))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    reservations.Add(new Reservation
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        CustomerName = reader["CustomerName"].ToString() ?? string.Empty,
                        Phone = reader["Phone"].ToString() ?? string.Empty,
                        ReservationDate = Convert.ToDateTime(reader["ReservationDate"]),
                        ReservationTime = (TimeSpan)reader["ReservationTime"],
                        GuestCount = Convert.ToInt32(reader["GuestCount"]),
                        TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                        PrepaidAmount = Convert.ToDecimal(reader["PrepaidAmount"]),
                        RemainingAmount = Convert.ToDecimal(reader["RemainingAmount"]),
                        ReservationStatus = reader["ReservationStatus"].ToString() ?? string.Empty,
                        PaymentStatus = reader["PaymentStatus"].ToString() ?? string.Empty,
                        CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
                    });
                }
            }
        }

        // თითოეული ჯავშნისთვის items ცალკე წამოვიღოთ (JOIN MenuItem-თან სახელისთვის)
        foreach (var reservation in reservations)
        {
            reservation.Items = GetItemsByReservationId(reservation.Id);
        }

        return reservations;
    }

    // ==========================================
    // GetById — ერთი კონკრეტული ჯავშანი
    // ==========================================
    public Reservation? GetById(int id)
    {
        using (var connection = (SqlConnection)_databaseService.GetConnection())
        {
            connection.Open();

            string query = @"
                SELECT Id, CustomerName, Phone, ReservationDate, ReservationTime, GuestCount,
                       TotalAmount, PrepaidAmount, RemainingAmount, ReservationStatus, PaymentStatus, CreatedAt
                FROM Reservation
                WHERE Id = @Id";

            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@Id", id);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var reservation = new Reservation
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            CustomerName = reader["CustomerName"].ToString() ?? string.Empty,
                            Phone = reader["Phone"].ToString() ?? string.Empty,
                            ReservationDate = Convert.ToDateTime(reader["ReservationDate"]),
                            ReservationTime = (TimeSpan)reader["ReservationTime"],
                            GuestCount = Convert.ToInt32(reader["GuestCount"]),
                            TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                            PrepaidAmount = Convert.ToDecimal(reader["PrepaidAmount"]),
                            RemainingAmount = Convert.ToDecimal(reader["RemainingAmount"]),
                            ReservationStatus = reader["ReservationStatus"].ToString() ?? string.Empty,
                            PaymentStatus = reader["PaymentStatus"].ToString() ?? string.Empty,
                            CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
                        };
                        reader.Close();
                        reservation.Items = GetItemsByReservationId(reservation.Id);
                        return reservation;
                    }
                }
            }
        }

        return null;
    }

    // ==========================================
    // Helper — items + MenuItem სახელი (JOIN)
    // ==========================================
    private List<ReservationItem> GetItemsByReservationId(int reservationId)
    {
        var items = new List<ReservationItem>();

        using (var connection = (SqlConnection)_databaseService.GetConnection())
        {
            connection.Open();

            string query = @"
                SELECT ri.Id, ri.ReservationId, ri.MenuItemId, ri.Quantity, ri.UnitPrice, ri.LineTotal,
                       mi.NameKa AS MenuItemName
                FROM ReservationItem ri
                INNER JOIN MenuItem mi ON ri.MenuItemId = mi.Id
                WHERE ri.ReservationId = @ReservationId";

            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@ReservationId", reservationId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new ReservationItem
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            ReservationId = Convert.ToInt32(reader["ReservationId"]),
                            MenuItemId = Convert.ToInt32(reader["MenuItemId"]),
                            Quantity = Convert.ToInt32(reader["Quantity"]),
                            UnitPrice = Convert.ToDecimal(reader["UnitPrice"]),
                            LineTotal = Convert.ToDecimal(reader["LineTotal"]),
                            MenuItemName = reader["MenuItemName"].ToString() ?? string.Empty
                        });
                    }
                }
            }
        }

        return items;
    }
}