namespace DbAdmin
{
  public interface IDBProvisioningService
  {
    Task<bool> CreateDbAsync(string dbName);
    Task DropDatabaseAsync(string dbName);
  }
}
