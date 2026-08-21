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
    public CreateReservationResult Create(Reservation reservation, List<ReservationItem> items, int durationHours, int holdMinutes)
    {
        using (var connection = (SqlConnection)_databaseService.GetConnection())
        {
            connection.Open();
            using (var transaction = connection.BeginTransaction(System.Data.IsolationLevel.Serializable))
            {
                try
                {
                    var newStart = reservation.ReservationDate.Date + reservation.ReservationTime;
                    var newEnd = newStart.AddHours(durationHours);

                    // ✅ საბოლოო, ტრანზაქციის შიგნით შემოწმება — race condition-ისგან დაცვა
                    // UPDLOCK + HOLDLOCK უზრუნველყოფს, რომ ამ ტრანზაქციის დასრულებამდე
                    // არავითარ სხვა პარალელურ მოთხოვნას არ შეეძლოს იმავე მაგიდის იგივე
                    // დროის მონაკვეთის "დანახვა" თავისუფლად
                    string checkQuery = @"
                    SELECT COUNT(*) FROM Reservation WITH (UPDLOCK, HOLDLOCK)
                    WHERE TableId = @TableId
                      AND (
                          ReservationStatus IN ('Confirmed', 'InProgress')
                          OR (ReservationStatus = 'Pending' AND CreatedAt >= DATEADD(MINUTE, -@HoldMinutes, GETDATE()))
                      )
                      AND (CAST(ReservationDate AS DATETIME) + CAST(ReservationTime AS DATETIME)) < @NewEnd
                      AND EstimatedEndDateTime > @NewStart";

                    int conflictCount;
                    using (var checkCmd = new SqlCommand(checkQuery, connection, transaction))
                    {
                        checkCmd.Parameters.AddWithValue("@TableId", reservation.TableId!.Value);
                        checkCmd.Parameters.AddWithValue("@HoldMinutes", holdMinutes);
                        checkCmd.Parameters.AddWithValue("@NewStart", newStart);
                        checkCmd.Parameters.AddWithValue("@NewEnd", newEnd);
                        conflictCount = (int)checkCmd.ExecuteScalar();
                    }

                    if (conflictCount > 0)
                    {
                        transaction.Rollback();
                        return new CreateReservationResult
                        {
                            Success = false,
                            ErrorMessage = "სამწუხაროდ, ეს მაგიდა ამ დროისთვის უკვე დაჯავშნილია. გთხოვთ აირჩიოთ სხვა მაგიდა ან სხვა დრო."
                        };
                    }

                    int reservationId;

                    string insertReservation = @"
                    INSERT INTO Reservation 
                        (CustomerName, Phone, ReservationDate, ReservationTime, GuestCount,
                         TotalAmount, PrepaidAmount, RemainingAmount, ReservationStatus, CreatedAt,
                         IsActive, DepositPaymentStatus, FinalPaymentStatus, TableId, ActualStartTime, ActualEndTime,
                         EstimatedEndDateTime)
                    OUTPUT INSERTED.Id
                    VALUES 
                        (@CustomerName, @Phone, @ReservationDate, @ReservationTime, @GuestCount,
                         @TotalAmount, @PrepaidAmount, @RemainingAmount, @ReservationStatus, @CreatedAt,
                         @IsActive, @DepositPaymentStatus, @FinalPaymentStatus, @TableId, NULL, NULL,
                         @EstimatedEndDateTime)";

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
                        cmd.Parameters.AddWithValue("@ReservationStatus", "Pending");
                        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                        cmd.Parameters.AddWithValue("@IsActive", true);
                        cmd.Parameters.AddWithValue("@DepositPaymentStatus", "Unpaid");
                        cmd.Parameters.AddWithValue("@FinalPaymentStatus", "Unpaid");
                        cmd.Parameters.AddWithValue("@TableId", reservation.TableId!.Value);
                        cmd.Parameters.AddWithValue("@EstimatedEndDateTime", newEnd);

                        reservationId = (int)cmd.ExecuteScalar();
                    }

                    string insertItem = @"
                    INSERT INTO ReservationItem (ReservationId, MenuItemId, Quantity, UnitPrice, LineTotal, NameSnapshot)
                    VALUES (@ReservationId, @MenuItemId, @Quantity, @UnitPrice, @LineTotal, @NameSnapshot)";

                    foreach (var item in items)
                    {
                        using (var cmd = new SqlCommand(insertItem, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@ReservationId", reservationId);
                            cmd.Parameters.AddWithValue("@MenuItemId", item.MenuItemId);
                            cmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                            cmd.Parameters.AddWithValue("@UnitPrice", item.UnitPrice);
                            cmd.Parameters.AddWithValue("@LineTotal", item.LineTotal);
                            cmd.Parameters.AddWithValue("@NameSnapshot", item.NameSnapshot);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();

                    return new CreateReservationResult
                    {
                        Success = true,
                        ReservationId = reservationId
                    };
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
    // GetAll
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

    // ==========================================
    // GetCompleted
    // ==========================================
    public List<Reservation> GetCompleted(string? searchTerm = null)
    {
        return GetByStatus("Completed", searchTerm);
    }

    // ==========================================
    // GetCancelled
    // ==========================================
    public List<Reservation> GetCancelled(string? searchTerm = null)
    {
        return GetByStatus("Cancelled", searchTerm);
    }

    // ==========================================
    // GetByDateAndStatuses — კონკრეტული თარიღის ჯავშნები, სტატუსების სიის მიხედვით
    // (გამოვიყენებთ "დღეს მაგიდები" ვიზუალისთვის)
    // ==========================================
    public List<Reservation> GetByDate(DateTime date)
    {
        var reservations = new List<Reservation>();

        using (var connection = (SqlConnection)_databaseService.GetConnection())
        {
            connection.Open();

            string query = @"
                SELECT r.Id, r.CustomerName, r.Phone, r.ReservationDate, r.ReservationTime, r.GuestCount,
                       r.TotalAmount, r.PrepaidAmount, r.RemainingAmount, r.ReservationStatus, r.CreatedAt,
                       r.IsActive, r.DepositPaymentStatus, r.FinalPaymentStatus, r.DepositPaidAt, r.FinalPaidAt,
                       r.TableId, r.ActualStartTime, r.ActualEndTime,
                       t.TableNumber, t.Capacity AS TableCapacity
                FROM Reservation r
                LEFT JOIN [Table] t ON r.TableId = t.Id
                WHERE r.ReservationDate = @Date
                ORDER BY r.ReservationTime ASC";

            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@Date", date.Date);

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
    // GetByFilter — IsActive-ის მიხედვით, search-ითაც
    // ==========================================
    private List<Reservation> GetByFilter(bool? isActive, string? searchTerm = null)
    {
        var reservations = new List<Reservation>();

        using (var connection = (SqlConnection)_databaseService.GetConnection())
        {
            connection.Open();

            string query = @"
                SELECT r.Id, r.CustomerName, r.Phone, r.ReservationDate, r.ReservationTime, r.GuestCount,
                       r.TotalAmount, r.PrepaidAmount, r.RemainingAmount, r.ReservationStatus, r.CreatedAt,
                       r.IsActive, r.DepositPaymentStatus, r.FinalPaymentStatus, r.DepositPaidAt, r.FinalPaidAt,
                       r.TableId, r.ActualStartTime, r.ActualEndTime,
                       t.TableNumber, t.Capacity AS TableCapacity
                FROM Reservation r
                LEFT JOIN [Table] t ON r.TableId = t.Id
                WHERE 1 = 1";

            if (isActive.HasValue)
            {
                query += " AND r.IsActive = @IsActive";
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query += " AND (r.CustomerName LIKE @SearchTerm OR r.Phone LIKE @SearchTerm)";
            }

            query += " ORDER BY r.ReservationDate ASC, r.ReservationTime ASC";

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
    // GetByStatus
    // ==========================================
    private List<Reservation> GetByStatus(string status, string? searchTerm = null)
    {
        var reservations = new List<Reservation>();

        using (var connection = (SqlConnection)_databaseService.GetConnection())
        {
            connection.Open();

            string query = @"
                SELECT r.Id, r.CustomerName, r.Phone, r.ReservationDate, r.ReservationTime, r.GuestCount,
                       r.TotalAmount, r.PrepaidAmount, r.RemainingAmount, r.ReservationStatus, r.CreatedAt,
                       r.IsActive, r.DepositPaymentStatus, r.FinalPaymentStatus, r.DepositPaidAt, r.FinalPaidAt,
                       r.TableId, r.ActualStartTime, r.ActualEndTime,
                       t.TableNumber, t.Capacity AS TableCapacity
                FROM Reservation r
                LEFT JOIN [Table] t ON r.TableId = t.Id
                WHERE r.ReservationStatus = @Status";

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query += " AND (r.CustomerName LIKE @SearchTerm OR r.Phone LIKE @SearchTerm)";
            }

            query += " ORDER BY r.ReservationDate DESC, r.ReservationTime DESC";

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
                SELECT r.Id, r.CustomerName, r.Phone, r.ReservationDate, r.ReservationTime, r.GuestCount,
                       r.TotalAmount, r.PrepaidAmount, r.RemainingAmount, r.ReservationStatus, r.CreatedAt,
                       r.IsActive, r.DepositPaymentStatus, r.FinalPaymentStatus, r.DepositPaidAt, r.FinalPaidAt,
                       r.TableId, r.ActualStartTime, r.ActualEndTime,
                       t.TableNumber, t.Capacity AS TableCapacity
                FROM Reservation r
                LEFT JOIN [Table] t ON r.TableId = t.Id
                WHERE r.Id = @Id";

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
    // MarkDepositPaid — Pending → Confirmed
    // ==========================================
    public void MarkDepositPaid(int reservationId)
    {
        using (var connection = (SqlConnection)_databaseService.GetConnection())
        {
            connection.Open();

            string query = @"
                UPDATE Reservation
                SET DepositPaymentStatus = 'Paid',
                    DepositPaidAt = @PaidAt,
                    ReservationStatus = 'Confirmed'
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
    // ✅ AssignTable — მაგიდის მინიჭება (Admin Panel-იდან)
    // ==========================================
    public void AssignTable(int reservationId, int tableId)
    {
        using (var connection = (SqlConnection)_databaseService.GetConnection())
        {
            connection.Open();

            string query = "UPDATE Reservation SET TableId = @TableId WHERE Id = @Id";

            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@Id", reservationId);
                cmd.Parameters.AddWithValue("@TableId", tableId);
                cmd.ExecuteNonQuery();
            }
        }
    }

    // ==========================================
    // ✅ StartReservation — "მაგიდა დაკავებულია / დაწყება"
    // Confirmed → InProgress, ActualStartTime დაფიქსირდეს
    // ==========================================
    public void StartReservation(int reservationId)
    {
        using (var connection = (SqlConnection)_databaseService.GetConnection())
        {
            connection.Open();

            string query = @"
                UPDATE Reservation
                SET ReservationStatus = 'InProgress',
                    ActualStartTime = @StartTime
                WHERE Id = @Id";

            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@Id", reservationId);
                cmd.Parameters.AddWithValue("@StartTime", DateTime.Now);
                cmd.ExecuteNonQuery();
            }
        }
    }

    // ==========================================
    // MarkFinalPaid — InProgress → Completed
    // ActualEndTime დაფიქსირდეს, მაგიდა თავისუფლდება (TableId რჩება ისტორიისთვის, 
    // მაგრამ IsActive=0-ის და status=Completed-ის გამო აღარ ითვლება დაკავებულად)
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
                    FinalPaidAt = @PaidAt,
                    ActualEndTime = @EndTime
                WHERE Id = @Id";

            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@Id", reservationId);
                cmd.Parameters.AddWithValue("@PaidAt", DateTime.Now);
                cmd.Parameters.AddWithValue("@EndTime", DateTime.Now);
                cmd.ExecuteNonQuery();
            }
        }
    }

    // ==========================================
    // CancelReservation — გაუქმება
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

            // ✅ INNER JOIN აღარ გვჭირდება — სახელს ვკითხულობთ NameSnapshot-იდან,
            // ეს ასევე ნიშნავს, რომ თუ MenuItem შემდგომში წაიშლება (თეორიულად), 
            // ისტორია მაინც უცვლელი დარჩება
            string query = @"
            SELECT Id, ReservationId, MenuItemId, Quantity, UnitPrice, LineTotal, NameSnapshot
            FROM ReservationItem
            WHERE ReservationId = @ReservationId";

            using (var cmd = new SqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@ReservationId", reservationId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string nameSnapshot = reader["NameSnapshot"].ToString() ?? string.Empty;

                        items.Add(new ReservationItem
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            ReservationId = Convert.ToInt32(reader["ReservationId"]),
                            MenuItemId = Convert.ToInt32(reader["MenuItemId"]),
                            Quantity = Convert.ToInt32(reader["Quantity"]),
                            UnitPrice = Convert.ToDecimal(reader["UnitPrice"]),
                            LineTotal = Convert.ToDecimal(reader["LineTotal"]),
                            NameSnapshot = nameSnapshot,
                            MenuItemName = nameSnapshot // ✅ Admin View-ები უცვლელად აგრძელებენ მუშაობას
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
            ReservationStatus = reader["ReservationStatus"].ToString() ?? "Pending",
            CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
            IsActive = Convert.ToBoolean(reader["IsActive"]),
            DepositPaymentStatus = reader["DepositPaymentStatus"].ToString() ?? "Unpaid",
            FinalPaymentStatus = reader["FinalPaymentStatus"].ToString() ?? "Unpaid",
            DepositPaidAt = reader["DepositPaidAt"] == DBNull.Value ? null : Convert.ToDateTime(reader["DepositPaidAt"]),
            FinalPaidAt = reader["FinalPaidAt"] == DBNull.Value ? null : Convert.ToDateTime(reader["FinalPaidAt"]),
            TableId = reader["TableId"] == DBNull.Value ? null : Convert.ToInt32(reader["TableId"]),
            ActualStartTime = reader["ActualStartTime"] == DBNull.Value ? null : Convert.ToDateTime(reader["ActualStartTime"]),
            ActualEndTime = reader["ActualEndTime"] == DBNull.Value ? null : Convert.ToDateTime(reader["ActualEndTime"]),
            TableNumber = reader["TableNumber"] == DBNull.Value ? null : Convert.ToInt32(reader["TableNumber"]),
            TableCapacity = reader["TableCapacity"] == DBNull.Value ? null : Convert.ToInt32(reader["TableCapacity"])
        };
    }
}