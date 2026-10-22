using System.Security.Claims;

namespace mt_admin;

/// <summary>
/// mt_admin inside a standalone TreeAlarm VMS installation, switched on by MT_ADMIN_REALM: it then
/// manages that one realm's users, for that realm's administrators, and nothing else. Without the
/// variable it is the multi-tenant console it always was: customers, a realm per tenant, provisioning.
/// </summary>
public sealed record StandaloneMode(string Realm, string ClientId)
{
  public const string AdminPolicy = "RealmAdmin";

  public static StandaloneMode? FromEnvironment()
  {
    var realm = Environment.GetEnvironmentVariable("MT_ADMIN_REALM");
    return string.IsNullOrWhiteSpace(realm) ? null : new StandaloneMode(realm, Require("KEYCLOAK_CLIENT_ID"));
  }

  public static string Require(string name) =>
    Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
      ? value
      : throw new InvalidOperationException($"Required environment variable '{name}' is not set");

  // The administrators TreeAlarm VMS has: realm role admin. The realm is checked too, because the token validation accepts a token from any realm of the
  // Keycloak (DynamicJwtBearerOptions finds the key by the token's own issuer).
  public bool IsAdmin(ClaimsPrincipal user) =>
    user.FindFirst("realm")?.Value == Realm
    && user.IsInRole(RoleConstants.admin);
}
