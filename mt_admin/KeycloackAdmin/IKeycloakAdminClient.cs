using Keycloak.Net.Models.Roles;
using Keycloak.Net.Models.Users;

namespace KeycloackAdmin
{
  public interface IKeycloakAdminClient
  {
    Task<bool> CreateRealmAsync(string realmName);
    Task<bool> CreateUserAsync(string realmName, string username, string password);
    Task<bool> CreateRoleAsync(string realmName, string roleName);
    Task<bool> AssignRolesToUserAsync(string realmName, string username, IEnumerable<Role> roles);

    Task<IEnumerable<User>> GetUsersAsync(string realmName);
    Task<IEnumerable<Role>> GetUserRolesAsync(string realmName, string userId);
  }
}
