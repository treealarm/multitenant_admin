using Keycloak.Net.Core.Models.Root;
using Keycloak.Net.Models.Roles;
using Keycloak.Net.Models.Users;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace KeycloackAdmin
{
  public interface IKeycloakAdminClient
  {
    public static RsaSecurityKey BuildRSAKey(string publicKeyJWT)
    {
      RSA rsa = RSA.Create();

      if (!string.IsNullOrEmpty(publicKeyJWT))
      {
        rsa.ImportSubjectPublicKeyInfo(
            source: Convert.FromBase64String(publicKeyJWT),
            bytesRead: out _
        );
      }

      var IssuerSigningKey = new RsaSecurityKey(rsa);

      return IssuerSigningKey;
    }
    Task<bool> IsRealmExistAsync(string realmName);
    Task<bool> CreateRealmAsync(string realmName);
    Task<bool> DeleteRealmAsync(string realmName);
    Task<bool> AddRealmToCustomerAsync(string realmName, string customerUserName, string customerRealmName);
    Task<bool> RemoveRealmFromCustomerAsync(string realmName, string customerUserName, string customerRealmName);
    Task<bool> CreateUserAsync(string realmName, string username, string password, string email);
    Task<bool> CreateRoleAsync(string realmName, string roleName);
    Task<bool> AssignRolesToUserAsync(string realmName, string username, IEnumerable<Role> roles);

    Task<IEnumerable<User>> GetUsersAsync(string realmName);
    Task<IEnumerable<Role>> GetUserRolesAsync(string realmName, string userName);
    Task<bool> DeleteUserAsync(string realmName, string userТфьу);
    Task<RsaSecurityKey?> GetRealmPublicKeyAsync(string realm, string? kid = null);
    Task<Token> GetTokenAsync(string realm, string clientId, string username, string password);

    Task<bool> IsTokenValid(string token);

    Task<IEnumerable<ComponentEx>?> GetRealmComponents(string realm);
    Task<bool> EnableRealmUnmanagetAttribute(string realm);
  }
}
