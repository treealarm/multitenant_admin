using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeycloackAdmin
{
  public interface IKeycloakAdminClient
  {
    Task<bool> CreateRealmAsync(string realmName);
    Task<bool> CreateUserAsync(string realmName, string username, string password);
    Task<bool> CreateRoleAsync(string realmName, string roleName);
  }
}
