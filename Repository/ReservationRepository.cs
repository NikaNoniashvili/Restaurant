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

                    string insertReservation = @"
                        INSERT INTO Reservation 
                            (CustomerName, Phone, ReservationDate, ReservationTime, GuestCount,
                             TotalAmount, PrepaidAmount, RemainingAmount, ReservationStatus, CreatedAt,
                             IsActive, DepositPaymentStatus, FinalPaymentStatus)
                        OUTPUT INSERTED.Id
                        VALUES 
                            (@CustomerName, @Phone, @ReservationDate, @ReservationTime, @GuestCount,
                             @TotalAmount, @PrepaidAmount, @RemainingAmount, @ReservationStatus, @CreatedAt,
                             @IsActive, @DepositPaymentStatus, @FinalPaymentStatus)";

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
                        cmd.Parameters.AddWithValue("@ReservationStatus", "Active");
                        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                        cmd.Parameters.AddWithValue("@IsActive", true);
                        cmd.Parameters.AddWithValue("@DepositPaymentStatus", "Unpaid");
                        cmd.Parameters.AddWithValue("@FinalPaymentStatus", "Unpaid");

                        reservationId = (int)cmd.ExecuteScalar();
                    }

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
    // GetAll — ყველა ჯავშანი
    // ==========================================
    public List<Reservation> GetAll()
    {
        return GetByFilter(null);
    }

    // ==========================================
    // GetActive — IsActive = true
    // ==========================================
    public List<Reservation> GetActive(string? searchTerm = null)
    {
        return GetByFilter(true, searchTerm);
    }

    public List<Reservation> GetCompleted(string? searchTerm = null)
    {
        return GetByStatus("Completed", searchTerm);
    }

    public List<Reservation> GetCancelled(string? searchTerm = null)
    {
        return GetByStatus("Cancelled", searchTerm);
    }

    // ==========================================
    // საერთო private მეთოდი — filter: null = ყველა, true/false = IsActive-ის მიხედვით
    // ==========================================
    private List<Reservation> GetByFilter(bool? isActive, string? searchTerm = null)
    {
        var reservations = new List<Reservation>();

        using (var connection = (SqlConnection)_databaseService.GetConnection())
        {
            connection.Open();

            string query = @"
            SELECT Id, CustomerName, Phone, ReservationDate, ReservationTime, GuestCount,
                   TotalAmount, PrepaidAmount, RemainingAmount, ReservationStatus, CreatedAt,
                   IsActive, DepositPaymentStatus, FinalPaymentStatus, DepositPaidAt, FinalPaidAt
            FROM Reservation
            WHERE 1 = 1";

            if (isActive.HasValue)
            {
                query += " AND IsActive = @IsActive";
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query += " AND (CustomerName LIKE @SearchTerm OR Phone LIKE @SearchTerm)";
            }

            query += " ORDER BY ReservationDate ASC, ReservationTime ASC";

            using (var cmd = new SqlCommand(query, connection))
            {
                if (isActive.HasValue)
                {
                    cmd.Parameters.AddWithValue("@IsActive", isActive.Value);
                }

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    cmd.Parameters.AddWithValue("@SearchTerm", "%" + searchTerm.Trim() + "%");
                }

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        reservations.Add(MapReservation(reader));
                    }
                }
            }
        }

        foreach (var reservation in reservations)
        {
            reservation.Items = GetItemsByReservationId(reservation.Id);
        }

        return reservations;
    }

    // ==========================================
    // საერთო private მეთოდი — კონკრეტული ReservationStatus-ის მიხედვით
    // ==========================================
    private List<Reservation> GetByStatus(string status, string? searchTerm = null)
    {
        var reservations = new List<Reservation>();

        using (var connection = (SqlConnection)_databaseService.GetConnection())
        {
            connection.Open();

            string query = @"
            SELECT Id, CustomerName, Phone, ReservationDate, ReservationTime, GuestCount,
                   TotalAmount, PrepaidAmount, RemainingAmount, ReservationStatus, CreatedAt,
                   IsActive, DepositPaymentStatus, FinalPaymentStatus, DepositPaidAt, FinalPaidAt
            FROM Reservation
            WHERE ReservationStatus = @Status";

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query += " AND (CustomerName LIKE @SearchTerm OR Phone LIKE @SearchTerm)";
            }

            query += " ORDER BY ReservationDate DESC, ReservationTime DESC";

            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@Status", status);

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    cmd.Parameters.AddWithValue("@SearchTerm", "%" + searchTerm.Trim() + "%");
                }

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        reservations.Add(MapReservation(reader));
                    }
                }
            }
        }

        foreach (var reservation in reservations)
        {
            reservation.Items = GetItemsByReservationId(reservation.Id);
        }

        return reservations;
    }

    // ==========================================
    // GetById
    // ==========================================
    public Reservation? GetById(int id)
    {
        using (var connection = (SqlConnection)_databaseService.GetConnection())
        {
            connection.Open();

            string query = @"
                              SELECT Id, CustomerName, Phone, ReservationDate, ReservationTime, GuestCount,
                                   TotalAmount, PrepaidAmount, RemainingAmount, ReservationStatus, CreatedAt,
                                   IsActive, DepositPaymentStatus, FinalPaymentStatus, DepositPaidAt, FinalPaidAt
                                        FROM Reservation
                WHERE Id = @Id";

            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@Id", id);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var reservation = MapReservation(reader);
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
    // MarkDepositPaid
    // ==========================================
    public void MarkDepositPaid(int reservationId)
    {
        using (var connection = (SqlConnection)_databaseService.GetConnection())
        {
            connection.Open();

            string query = @"
            UPDATE Reservation
            SET DepositPaymentStatus = 'Paid',
                DepositPaidAt = @PaidAt
            WHERE Id = @Id";

            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@Id", reservationId);
                cmd.Parameters.AddWithValue("@PaidAt", DateTime.Now);
                cmd.ExecuteNonQuery();
            }
        }
    }

    // ==========================================
    // MarkFinalPaid — სრული გადახდა → Completed
    // ==========================================
    public void MarkFinalPaid(int reservationId)
    {
        using (var connection = (SqlConnection)_databaseService.GetConnection())
        {
            connection.Open();

            string query = @"
            UPDATE Reservation
            SET FinalPaymentStatus = 'Paid',
                ReservationStatus = 'Completed',
                IsActive = 0,
                FinalPaidAt = @PaidAt
            WHERE Id = @Id";

            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@Id", reservationId);
                cmd.Parameters.AddWithValue("@PaidAt", DateTime.Now);
                cmd.ExecuteNonQuery();
            }
        }
    }

    // ==========================================
    // CancelReservation — გაუქმება → Cancelled
    // (გადახდის ისტორია უცვლელი რჩება)
    // ==========================================
    public void CancelReservation(int reservationId)
    {
        using (var connection = (SqlConnection)_databaseService.GetConnection())
        {
            connection.Open();

            string query = @"
                UPDATE Reservation
                SET ReservationStatus = 'Cancelled',
                    IsActive = 0
                WHERE Id = @Id";

            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@Id", reservationId);
                cmd.ExecuteNonQuery();
            }
        }
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

    // ==========================================
    // GetMonthlyStatistics — თვის მიხედვით სტატისტიკა კონკრეტული წლისთვის
    // ==========================================
    public List<MonthlyStatRow> GetMonthlyStatistics(int year)
    {
        var stats = new List<MonthlyStatRow>();

        using (var connection = (SqlConnection)_databaseService.GetConnection())
        {
            connection.Open();

            // წინასწარი გადახდები — თვის მიხედვით, DepositPaidAt-ის თარიღით
            string depositQuery = @"
            SELECT MONTH(DepositPaidAt) AS MonthNum, 
                   SUM(PrepaidAmount) AS TotalDeposit
            FROM Reservation
            WHERE DepositPaymentStatus = 'Paid' 
              AND DepositPaidAt IS NOT NULL
              AND YEAR(DepositPaidAt) = @Year
            GROUP BY MONTH(DepositPaidAt)";

            var depositByMonth = new Dictionary<int, decimal>();
            using (var cmd = new SqlCommand(depositQuery, connection))
            {
                cmd.Parameters.AddWithValue("@Year", year);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int month = Convert.ToInt32(reader["MonthNum"]);
                        decimal total = Convert.ToDecimal(reader["TotalDeposit"]);
                        depositByMonth[month] = total;
                    }
                }
            }

            // სრული გადახდები — თვის მიხედვით, FinalPaidAt-ის თარიღით
            string finalQuery = @"
            SELECT MONTH(FinalPaidAt) AS MonthNum, 
                   SUM(RemainingAmount) AS TotalFinal,
                   COUNT(*) AS CompletedCount,
                   SUM(GuestCount) AS TotalGuests
            FROM Reservation
            WHERE FinalPaymentStatus = 'Paid' 
              AND FinalPaidAt IS NOT NULL
              AND YEAR(FinalPaidAt) = @Year
            GROUP BY MONTH(FinalPaidAt)";

            var finalByMonth = new Dictionary<int, (decimal total, int count, int guests)>();
            using (var cmd = new SqlCommand(finalQuery, connection))
            {
                cmd.Parameters.AddWithValue("@Year", year);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int month = Convert.ToInt32(reader["MonthNum"]);
                        decimal total = Convert.ToDecimal(reader["TotalFinal"]);
                        int count = Convert.ToInt32(reader["CompletedCount"]);
                        int guests = reader["TotalGuests"] == DBNull.Value ? 0 : Convert.ToInt32(reader["TotalGuests"]);
                        finalByMonth[month] = (total, count, guests);
                    }
                }
            }

            // 12 თვის საბოლოო აწყობა
            for (int month = 1; month <= 12; month++)
            {
                decimal depositTotal = depositByMonth.ContainsKey(month) ? depositByMonth[month] : 0;
                var finalData = finalByMonth.ContainsKey(month) ? finalByMonth[month] : (0, 0, 0);

                stats.Add(new MonthlyStatRow
                {
                    Month = month,
                    TotalRevenue = depositTotal + finalData.Item1,
                    CompletedReservations = finalData.Item2,
                    TotalGuests = finalData.Item3
                });
            }
        }

        return stats;
    }

    // ==========================================
    // Helper — reader-იდან Reservation object აწყობა
    // ==========================================
    private Reservation MapReservation(SqlDataReader reader)
    {
        return new Reservation
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
            ReservationStatus = reader["ReservationStatus"].ToString() ?? "Active",
            CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
            IsActive = Convert.ToBoolean(reader["IsActive"]),
            DepositPaymentStatus = reader["DepositPaymentStatus"].ToString() ?? "Unpaid",
            FinalPaymentStatus = reader["FinalPaymentStatus"].ToString() ?? "Unpaid",
            DepositPaidAt = reader["DepositPaidAt"] == DBNull.Value ? null : Convert.ToDateTime(reader["DepositPaidAt"]),
            FinalPaidAt = reader["FinalPaidAt"] == DBNull.Value ? null : Convert.ToDateTime(reader["FinalPaidAt"])
        };
    }
}