namespace DbAdmin
{
  public interface IDBProvisioningService
  {
    Task CreateDbAsync(string dbName);
    Task DropDatabaseAsync(string dbName);
  }
}
