using Keycloak.Net;
using Keycloak.Net.Models.Clients;
using Keycloak.Net.Models.RealmsAdmin;
using Keycloak.Net.Models.Roles;
using Keycloak.Net.Models.Users;
using System.Linq;
using System.Threading.Tasks;

namespace KeycloackAdmin
{
  public class KeycloakAdminClient : IKeycloakAdminClient
  {
    private readonly KeycloakClient _client;

    public KeycloakAdminClient(string keycloakUrl, string adminUser, string adminPassword)
    {
      var options = new KeycloakOptions(authenticationRealm: "master");
      _client = new KeycloakClient(keycloakUrl, adminUser, adminPassword, options);
    }

    // Создание realm
    public async Task<bool> CreateRealmAsync(string realmName)
    {
      Realm? existing = null;

      try
      {
        existing = await _client.GetRealmAsync(realmName);

        if (existing != null)
        {
          return false;
        }
      }
      catch
      {
        
      }
      

      var realm = new Realm
      {
        Id = realmName,
        _Realm = realmName,
        Enabled = true,
        DefaultRoles = new[] { "offline_access", "uma_authorization" }
      };

      return await _client.ImportRealmAsync(realmName, realm);
    }

    // Создание роли
    public async Task<bool> CreateRoleAsync(string realmName, string roleName)
    {
      var roles = await _client.GetRoleByNameAsync(realmName, roleName);

      if (roles != null)
      {
        return false;
      }

      var role = new Role { Name = roleName };
      return await _client.CreateRoleAsync(realmName, role);
    }

    public async Task<bool> CreateUserAsync(string realmName, string username, string password)
    {
      var users = await _client.GetUsersAsync(realmName, username: username);

      var user = users?.Where(u => u.UserName == username).FirstOrDefault();
      if (user != null) return false;

      user = new User
      {
        UserName = username,
        Enabled = true,
        Credentials = new[]
          {
                    new Credentials { Type = "password", Value = password, Temporary = false }
          }
      };

      return await _client.CreateUserAsync(realmName, user);
    }

    // Отдельный метод для назначения ролей пользователю
    public async Task<bool> AssignRolesToUserAsync(string realmName, string username, IEnumerable<Role> roles)
    {
      var users = await _client.GetUsersAsync(realmName, username: username);

      var user = users?.Where(u=>u.UserName == username).FirstOrDefault();
      if (user == null) return false;
      // Назначаем роли пользователю
      return await _client.AddRealmRoleMappingsToUserAsync(
        realmName, user.Id, roles);
    }
  }
}
