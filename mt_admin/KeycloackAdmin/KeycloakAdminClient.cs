using Keycloak.Net;
using Keycloak.Net.Models.Clients;
using Keycloak.Net.Models.RealmsAdmin;
using Keycloak.Net.Models.Roles;
using Keycloak.Net.Models.Users;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace KeycloackAdmin
{
  public class KeycloakAdminClient : IKeycloakAdminClient
  {
    private readonly KeycloakClient _client;

    public KeycloakAdminClient(string keycloakUrl, string adminUser, string adminPassword)
    {
      var keycloakRealm = Environment.GetEnvironmentVariable("KEYCLOAK_REALM") ?? "master";

      var options = new KeycloakOptions(authenticationRealm: keycloakRealm);
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
        DefaultRoles = new[] { "offline_access", "uma_authorization", "admin","anon","user", "default-roles-"+ realmName }
      };

      return await _client.ImportRealmAsync(realmName, realm);
    }

    // Создание роли
    public async Task<bool> CreateRoleAsync(string realmName, string roleName)
    {
      Role? role = null;

      try
      {
        role = await _client.GetRoleByNameAsync(realmName, roleName);
        if (role != null)
        {
          return false;
        }
      }
      catch(Exception)
      {

      }
      

      role = new Role { Name = roleName };
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

    public async Task<bool> AssignRolesToUserAsync(string realmName, string username, IEnumerable<Role> roles)
    {
      var users = await _client.GetUsersAsync(realmName, username: username);
      var user = users?.FirstOrDefault(u => u.UserName == username);
      if (user == null)
        return false;

      var fullRoles = new List<Role>();
      foreach (var role in roles)
      {
        try
        {
          var existingRole = await _client.GetRoleByNameAsync(realmName, role.Name);
          if (existingRole != null)
            fullRoles.Add(existingRole);
        }
        catch
        {
          // ignore missing roles
        }
      }

      if (!fullRoles.Any())
        return false;

      return await _client.AddRealmRoleMappingsToUserAsync(realmName, user.Id, fullRoles);
    }

    public async Task<IEnumerable<User>> GetUsersAsync(string realmName)
    {
      var users = await _client.GetUsersAsync(realmName);
        return users ?? Enumerable.Empty<User>();
    }

    public async Task<IEnumerable<Role>> GetUserRolesAsync(string realmName, string userId)
    {
      var roles = await _client.GetRealmRoleMappingsForUserAsync(realmName, userId);
        return roles ?? Enumerable.Empty<Role>();
    }
  }
}
