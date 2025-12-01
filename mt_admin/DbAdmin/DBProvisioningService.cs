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
      _pgHost = Environment.GetEnvironmentVariable("MapDatabase__PgHost") ?? "localhost";
      var portStr = Environment.GetEnvironmentVariable("MapDatabase__PgPort") ?? "5432";
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

    public async Task ProvisionRealmAsync(string realmName)
    {
      await CreateDatabaseAsync(realmName);
      await ApplySqlScriptsAsync(realmName);
    }

    private async Task CreateDatabaseAsync(string dbName)
    {
      await using var conn = new NpgsqlConnection(_masterConnectionString);
      await conn.OpenAsync();

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

      var scriptsPath = Path.Combine(AppContext.BaseDirectory, "init_files", "initdb", "sql_scripts");
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

    private string RemovePsqlCommands(string sql)
    {
      return string.Join("\n",
          sql.Split('\n')
              .Where(line => !line.TrimStart().StartsWith("\\"))
      );
    }
  }
}
