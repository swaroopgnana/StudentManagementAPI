using Microsoft.Data.SqlClient;

namespace StudentManagementAPI.Data
{
    public static class DatabaseInitializer
    {
        private static string? _workingConnectionString;

        public static string? WorkingConnectionString => _workingConnectionString;

        private static readonly string[] CandidateInstances = new[]
        {
            @"(localdb)\MSSQLLocalDB",
            @".\SQLEXPRESS",
            @"localhost",
            @"."
        };

        public static async Task InitializeAsync(IConfiguration configuration, ILogger logger)
        {
            string? configuredConnStr = configuration.GetConnectionString("DefaultConnection");

            // Build list of connection strings to try
            var candidates = new List<string>();
            if (!string.IsNullOrWhiteSpace(configuredConnStr))
            {
                candidates.Add(configuredConnStr);
            }

            foreach (var instance in CandidateInstances)
            {
                string candidate = $"Data Source={instance};Initial Catalog=StudentAPI;Integrated Security=True;TrustServerCertificate=True;";
                if (!candidates.Contains(candidate, StringComparer.OrdinalIgnoreCase))
                {
                    candidates.Add(candidate);
                }
            }

            foreach (var connStr in candidates)
            {
                try
                {
                    var builder = new SqlConnectionStringBuilder(connStr);
                    string targetDb = string.IsNullOrWhiteSpace(builder.InitialCatalog) ? "StudentAPI" : builder.InitialCatalog;

                    // 1. Connect to master to check/create database
                    builder.InitialCatalog = "master";
                    builder.ConnectTimeout = 3; // Fast timeout for candidate discovery

                    await using (var masterConn = new SqlConnection(builder.ConnectionString))
                    {
                        await masterConn.OpenAsync();

                        bool dbExists;
                        using (var checkCmd = new SqlCommand("SELECT COUNT(*) FROM sys.databases WHERE name = @DbName", masterConn))
                        {
                            checkCmd.Parameters.AddWithValue("@DbName", targetDb);
                            var result = await checkCmd.ExecuteScalarAsync();
                            dbExists = Convert.ToInt32(result) > 0;
                        }

                        if (!dbExists)
                        {
                            // In SQL Server, CREATE DATABASE must be the only statement in the batch!
                            using var createDbCmd = new SqlCommand($"CREATE DATABASE [{targetDb}]", masterConn);
                            await createDbCmd.ExecuteNonQueryAsync();
                            logger.LogInformation("Database '{DbName}' created successfully on {DataSource}.", targetDb, builder.DataSource);
                        }
                    }

                    // 2. Connect to target database to check/create table
                    builder.InitialCatalog = targetDb;
                    await using (var appConn = new SqlConnection(builder.ConnectionString))
                    {
                        await appConn.OpenAsync();

                        bool tableExists;
                        using (var checkCmd = new SqlCommand("SELECT COUNT(*) FROM sys.tables WHERE name = @TableName", appConn))
                        {
                            checkCmd.Parameters.AddWithValue("@TableName", "Students");
                            var result = await checkCmd.ExecuteScalarAsync();
                            tableExists = Convert.ToInt32(result) > 0;
                        }

                        if (!tableExists)
                        {
                            string createTableSql = @"
                                CREATE TABLE Students (
                                    Id INT IDENTITY(1,1) PRIMARY KEY,
                                    Name NVARCHAR(100) NOT NULL,
                                    Email NVARCHAR(100) NOT NULL,
                                    Course NVARCHAR(100) NOT NULL
                                );

                                INSERT INTO Students (Name, Email, Course) VALUES
                                ('Alice Johnson', 'alice.johnson@example.com', 'Computer Science'),
                                ('Bob Smith', 'bob.smith@example.com', 'Information Technology'),
                                ('Charlie Brown', 'charlie.brown@example.com', 'Data Science');";

                            using var createTableCmd = new SqlCommand(createTableSql, appConn);
                            await createTableCmd.ExecuteNonQueryAsync();
                            logger.LogInformation("Table 'Students' created and seeded successfully on {DataSource}.", builder.DataSource);
                        }
                    }

                    _workingConnectionString = builder.ConnectionString;
                    logger.LogInformation("Database initialized and connected to SQL Server at {DataSource}.", builder.DataSource);
                    return; // Successfully initialized!
                }
                catch
                {
                    // Failed on this instance, continue to next candidate
                }
            }

            logger.LogWarning("No reachable SQL Server instance found among candidates. Endpoints will use in-memory fallback until SQL Server is started.");
        }
    }
}
