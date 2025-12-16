using Npgsql;

namespace DbAdmin
{
  public class DBProvisioningService : IDBProvisioningService
  {
    private readonly string _masterConnectionString;
    private readonly string _postgresUser;
    private readonly string _postgresPassword;

    private readonly string _pgHost;
    private readonly int _pgPort;
    private readonly string _pgDB;

    public DBProvisioningService()
    {
      _pgHost = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost";
      var portStr = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
      if (!int.TryParse(portStr, out _pgPort))
        _pgPort = 5432;
      _postgresUser = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "keycloak";
      _postgresPassword = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "password";
      _pgDB = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "keycloak";
      

      // MASTER DB — для команд создания БД
      _masterConnectionString = new NpgsqlConnectionStringBuilder
      {
        Host = _pgHost,
        Port = _pgPort,
        Username = _postgresUser,
        Password = _postgresPassword,
        Database = _pgDB
      }.ConnectionString;
    }

    public async Task CreateDbAsync(string dbName)
    {
      await CreateDatabaseAsync(dbName);
      await ApplySqlScriptsAsync(dbName);
    }

    private async Task CreateDatabaseAsync(string dbName)
    {
      await using var conn = new NpgsqlConnection(_masterConnectionString);
      await conn.OpenAsync();

      // Проверяем, существует ли база
      await using var checkCmd = conn.CreateCommand();
      checkCmd.CommandText = "SELECT 1 FROM pg_database WHERE datname = @dbName";
      checkCmd.Parameters.AddWithValue("dbName", dbName);

      var exists = await checkCmd.ExecuteScalarAsync();
      if (exists != null)
      {
        Console.WriteLine($"Database {dbName} already exists, skipping creation.");
        return;
      }

      // Создаём базу
      await using var cmd = conn.CreateCommand();
      cmd.CommandText = $"CREATE DATABASE \"{dbName}\"";
      await cmd.ExecuteNonQueryAsync();

      Console.WriteLine($"Database {dbName} created");
    }


    private async Task ApplySqlScriptsAsync(string dbName)
    {
      string realmConnStr = new NpgsqlConnectionStringBuilder
      {
        Host = _pgHost,
        Port = _pgPort,
        Username = _postgresUser,
        Password = _postgresPassword,
        Database = dbName
      }.ConnectionString;

      var scriptsPath = Path.Combine(AppContext.BaseDirectory, "sql_scripts");
      var sqlFiles = Directory.GetFiles(scriptsPath, "*.sql")
                              .OrderBy(f => f)
                              .ToList();

      await using var conn = new NpgsqlConnection(realmConnStr);
      await conn.OpenAsync();

      foreach (var file in sqlFiles)
      {
        string sql = await File.ReadAllTextAsync(file);
        sql = RemovePsqlCommands(sql);

        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();

        Console.WriteLine($"Executed {Path.GetFileName(file)}");
      }
    }
    public async Task DropDatabaseAsync(string dbName)
    {
      await using var conn = new NpgsqlConnection(_masterConnectionString);
      await conn.OpenAsync();

      // Завершаем все активные подключения к базе
      await using (var cmd = conn.CreateCommand())
      {
        cmd.CommandText = $@"
                SELECT pg_terminate_backend(pid) 
                FROM pg_stat_activity 
                WHERE datname = @dbName AND pid <> pg_backend_pid();";
        cmd.Parameters.AddWithValue("dbName", dbName);
        await cmd.ExecuteNonQueryAsync();
      }

      // Удаляем базу, если существует
      await using (var cmd = conn.CreateCommand())
      {
        cmd.CommandText = $"DROP DATABASE IF EXISTS \"{dbName}\"";
        await cmd.ExecuteNonQueryAsync();
      }

      Console.WriteLine($"Database {dbName} dropped.");
    }
    private string RemovePsqlCommands(string sql)
    {
      return string.Join("\n",
          sql.Split('\n')
              .Where(line => !line.TrimStart().StartsWith("\\"))
      );
    }
  }
}
