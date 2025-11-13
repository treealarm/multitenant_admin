using KeycloackAdmin;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace mt_admin
{
  public class KeycloakTokenValidatorService : IKeycloakTokenValidator
  {
    private readonly IKeycloakAdminClient _kcClient;
    private readonly IMemoryCache _cache;
    private readonly Dictionary<string, RsaSecurityKey> _realmKeysCache = new();

    public KeycloakTokenValidatorService(IKeycloakAdminClient kcClient, IMemoryCache cache)
    {
      _kcClient = kcClient;
      _cache = cache;
    }

    public async Task<TokenValidationResult?> ValidateTokenAsync(string token)
    {
      if (string.IsNullOrWhiteSpace(token))
        return null;

      // Проверяем кэш токена
      //if (_cache.TryGetValue(token, out TokenValidationResult? cachedToken))
      //{
      //  if (cachedToken?.ExpiresAt > DateTime.UtcNow)
      //    return cachedToken;
      //}

      var handler = new JwtSecurityTokenHandler();
      var jwt = handler.ReadJwtToken(token);

      var iss = jwt.Issuer; // например, http://localhost:8080/realms/myrealm
      var kid = jwt.Header.Kid;

      // Получаем ключ с кэшем по (issuer, kid)
      var rsa = await GetRealmKeyAsync(iss, kid);
      if (rsa == null)
        return null;

      var validationParameters = new TokenValidationParameters
      {
        IssuerSigningKey = rsa,
        ValidIssuer = iss,
        ValidateIssuer = true,
        ValidateLifetime = true,
        ValidateAudience = false
      };

      try
      {
        var principal = handler.ValidateToken(token, validationParameters, out var validatedToken);

        // Извлекаем данные
        var username = principal.FindFirst("preferred_username")?.Value ?? "";
        var realm = iss.Split("/realms/")[1];
        var roles = principal.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        var result = new TokenValidationResult
        {
          Username = username,
          Realm = realm,
          Roles = roles,
          ExpiresAt = validatedToken.ValidTo
        };

        // Кэшируем результат токена
        _cache.Set(token, result, result.ExpiresAt);

        return result;
      }
      catch
      {
        return null;
      }
    }

    private async Task<RsaSecurityKey?> GetRealmKeyAsync(string issuer, string? kid)
    {
      // Используем кэш по ключу issuer+kid
      var cacheKey = $"{issuer}|{kid}";
      if (_realmKeysCache.TryGetValue(cacheKey, out var cachedKey))
        return cachedKey;

      // Получаем имя realm из issuer
      var parts = issuer.Split("/realms/");
      if (parts.Length != 2) return null;
      var realm = parts[1];

      // Получаем ключ через KeycloakAdminClient
      var rsa = await _kcClient.GetRealmPublicKeyAsync(realm, kid);
      if (rsa != null)
        _realmKeysCache[cacheKey] = rsa;

      return rsa;
    }
  }
}
