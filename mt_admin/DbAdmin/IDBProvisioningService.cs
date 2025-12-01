namespace DbAdmin
{
  public interface IDBProvisioningService
  {
    Task ProvisionRealmAsync(string realmName);
  }
}
