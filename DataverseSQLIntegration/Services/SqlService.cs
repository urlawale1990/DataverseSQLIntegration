using Microsoft.Data.SqlClient;

namespace DataverseSQLIntegration;

public static class SqlService
{
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("SqlConnectionString")!;

    // Save or update contact in SQL
    public static async Task UpsertContact(
        string? dataverseId, string fullName,
        string email, string? mobilePhone,
        string syncDirection)
    {
        using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();

        var cmd = new SqlCommand(@"
            IF EXISTS (SELECT 1 FROM Contacts WHERE Email = @Email)
                UPDATE Contacts
                SET FullName      = @FullName,
                    MobilePhone   = @MobilePhone,
                    DataverseId   = @DataverseId,
                    LastSyncedOn  = GETDATE(),
                    SyncDirection = @SyncDirection
                WHERE Email = @Email
            ELSE
                INSERT INTO Contacts
                    (DataverseId, FullName, Email, MobilePhone, SyncDirection)
                VALUES
                    (@DataverseId, @FullName, @Email, @MobilePhone, @SyncDirection)
        ", conn);

        cmd.Parameters.AddWithValue("@Email", email);
        cmd.Parameters.AddWithValue("@FullName", fullName);
        cmd.Parameters.AddWithValue("@MobilePhone",
            mobilePhone ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@DataverseId",
            dataverseId ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@SyncDirection", syncDirection);

        await cmd.ExecuteNonQueryAsync();
    }

    // Get contact from SQL by email
    public static async Task<SqlContact?> GetContactByEmail(string email)
    {
        using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();

        var cmd = new SqlCommand(@"
            SELECT DataverseId, FullName, Email,
                   MobilePhone, CreatedOn, LastSyncedOn, SyncDirection
            FROM Contacts
            WHERE Email = @Email", conn);

        cmd.Parameters.AddWithValue("@Email", email);

        using var reader = await cmd.ExecuteReaderAsync();
        if (!reader.HasRows) return null;

        await reader.ReadAsync();
        return new SqlContact
        {
            DataverseId = reader["DataverseId"]?.ToString(),
            FullName = reader["FullName"].ToString()!,
            Email = reader["Email"].ToString()!,
            MobilePhone = reader["MobilePhone"]?.ToString(),
            CreatedOn = reader["CreatedOn"] as DateTime?,
            LastSyncedOn = reader["LastSyncedOn"] as DateTime?,
            SyncDirection = reader["SyncDirection"]?.ToString()
        };
    }

    // Get all contacts from SQL
    public static async Task<List<SqlContact>> GetAllContacts()
    {
        var contacts = new List<SqlContact>();

        using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();

        var cmd = new SqlCommand(@"
            SELECT DataverseId, FullName, Email,
                   MobilePhone, CreatedOn, LastSyncedOn, SyncDirection
            FROM Contacts
            ORDER BY LastSyncedOn DESC", conn);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            contacts.Add(new SqlContact
            {
                DataverseId = reader["DataverseId"]?.ToString(),
                FullName = reader["FullName"].ToString()!,
                Email = reader["Email"].ToString()!,
                MobilePhone = reader["MobilePhone"]?.ToString(),
                CreatedOn = reader["CreatedOn"] as DateTime?,
                LastSyncedOn = reader["LastSyncedOn"] as DateTime?,
                SyncDirection = reader["SyncDirection"]?.ToString()
            });
        }

        return contacts;
    }
}