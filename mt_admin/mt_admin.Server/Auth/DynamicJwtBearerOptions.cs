using KeycloackAdmin;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace mt_admin
{


  public class DynamicJwtBearerOptions : IConfigureNamedOptions<JwtBearerOptions>
  {
    private readonly KeycloakConfig _config;
    private readonly IKeycloakAdminClient _kcAdmin;

    public DynamicJwtBearerOptions(KeycloakConfig config, IKeycloakAdminClient kcAdmin)
    {
      _config = config;
      _kcAdmin = kcAdmin;
    }

    public void Configure(string? name, JwtBearerOptions options)
    {
      options.RequireHttpsMetadata = false;
      options.TokenValidationParameters = new TokenValidationParameters
      {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
        {
          var handler = new JwtSecurityTokenHandler();
          var jwt = handler.ReadJwtToken(token); // разбор строки токена
          var issuer = jwt.Issuer; // "http://localhost:8080/realms/myrealm"

          if (string.IsNullOrEmpty(issuer))
            return Array.Empty<SecurityKey>();

          // Извлекаем realm из issuer
          var parts = issuer.Split("/realms/");
          if (parts.Length != 2) return Array.Empty<SecurityKey>();
          var realm = parts[1];

          // Получаем ключ по realm и kid
          var key = _kcAdmin.GetRealmPublicKeyAsync(realm, kid).GetAwaiter().GetResult();
          return key != null ? new[] { key } : Array.Empty<SecurityKey>();
        }
      };

      options.Events = new JwtBearerEvents
      {
        OnMessageReceived = context =>
        {
          //var token = context.Request.Headers["Authorization"].ToString()
          //              .Replace("Bearer ", "");
          //if (!string.IsNullOrEmpty(token))
          //{
          //  context.HttpContext.Items["access_token"] = token;
          //}
          return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
          return Task.CompletedTask;
        }
      };



    }


    public void Configure(JwtBearerOptions options) => Configure(Options.DefaultName, options);
  }

}
