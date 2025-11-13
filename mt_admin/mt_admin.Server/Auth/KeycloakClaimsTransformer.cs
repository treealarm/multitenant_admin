using Keycloak.Net.Models.RealmsAdmin;
using Keycloak.Net.Models.Roles;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Text.Json;

namespace mt_admin
{
  public class KeycloakClaimsTransformer : IClaimsTransformation
  {
    private readonly IKeycloakTokenValidator _validator;
    private class RealmAccess
    {
      public string[]? roles { get; set; }
    }
    public KeycloakClaimsTransformer(IKeycloakTokenValidator validator)
    {
      _validator = validator;
    }

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
      var claimsIdentity = principal.Identity as ClaimsIdentity;
      if (claimsIdentity == null || !claimsIdentity.IsAuthenticated)
        return Task.FromResult(principal);

      // Ищем блок realm_access (он хранит роли Keycloak)
      var realmAccessClaim = claimsIdentity.FindFirst(c => c.Type == "realm_access");
      var userName = claimsIdentity.FindFirst("preferred_username")?.Value;
      var issuer = claimsIdentity.FindFirst("iss")?.Value;

      if (realmAccessClaim != null)
      {
        try
        {
          // Парсим JSON вида: {"roles":["admin","user"]}
          var content = JsonSerializer.Deserialize<RealmAccess>(realmAccessClaim.Value);

          if (content?.roles != null)
          {
            foreach (var role in content.roles)
            {
              claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, role));
            }
          }
        }
        catch
        {
          // ignore parse errors
        }
      }

      // Добавляем имя пользователя
      if (!string.IsNullOrEmpty(userName))
      {
        claimsIdentity.AddClaim(new Claim(ClaimTypes.Name, userName));
      }

      // Добавляем realm
      if (!string.IsNullOrEmpty(issuer))
      {
        var parts = issuer.Split("/realms/");
        if (parts.Length > 1)
        {
          var realm = parts[1];
          claimsIdentity.AddClaim(new Claim("realm", realm));
        }
      }

      bool isMasterAdmin =
        claimsIdentity.HasClaim(c => c.Type == "iss" && c.Value.Contains("/realms/master")) &&
        claimsIdentity.HasClaim(c => c.Type == "azp" && c.Value == "admin-cli");
      if (isMasterAdmin)
      {
        claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, "create-realm"));
      }

      return Task.FromResult(principal);
    }
  }

}
