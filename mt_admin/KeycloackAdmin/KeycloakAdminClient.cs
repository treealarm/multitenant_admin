using Flurl.Http;
using Keycloak.Net;
using Keycloak.Net.Core.Models.Root;
using Keycloak.Net.Models.Clients;
using Keycloak.Net.Models.RealmsAdmin;
using Keycloak.Net.Models.Roles;
using Keycloak.Net.Models.Users;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace KeycloackAdmin
{
  public class KeycloakAdminClient : IKeycloakAdminClient
  {
    private readonly KeycloakClient _client;
    private readonly string _keycloakUrl;
    private readonly string _adminUser;
    private readonly string _adminPassword;
    private readonly string _keycloakRealm;
    public KeycloakAdminClient(string keycloakUrl, string adminUser, string adminPassword)
    {
      _keycloakUrl = keycloakUrl;
      _adminUser = adminUser;
      _adminPassword = adminPassword;

      _keycloakRealm = Environment.GetEnvironmentVariable("KEYCLOAK_REALM") ?? "master";

      var options = new KeycloakOptions(authenticationRealm: _keycloakRealm);
      _client = new KeycloakClient(keycloakUrl, adminUser, adminPassword, options);
    }

    public async Task<IEnumerable<ComponentEx>?> GetRealmComponents(string realm)
    {
      var json = await KeycloakRawHelper.GetComponentRawJsonAsync(
            keycloakUrl: _keycloakUrl,
            admin_realm: _keycloakRealm,
            realm: realm,
            clientId: "admin-cli",
            username: _adminUser,
            password: _adminPassword
        );

      var components = JsonSerializer.Deserialize<IEnumerable<ComponentEx>>(json);


      return components;
    }

    public async Task<bool> EnableRealmUnmanagetAttribute(string realm)
    {
      Realm? existing = null;

      try
      {
        existing = await _client.GetRealmAsync(realm);

        if (existing == null)
        {
          return false;
        }
      }
      catch
      {
        return false;
      }

      var components = await GetRealmComponents(realm);
      ComponentEx? component = null;
      if (components != null)
      {
        component = components.Where(c => c.ProviderId == "declarative-user-profile").FirstOrDefault();

        if (component != null)
        {
          var profileJsonString = component.Config["kc.user.profile.config"].FirstOrDefault();

          if (profileJsonString != null)
          {
            // 2) Десериализуем внутрь UserProfileConfigRoot
            var profileConfig = JsonSerializer.Deserialize<UserProfileConfigRoot>(profileJsonString);

            // 3) Проверяем unmanagedAttributePolicy
            var unmanagedEnabled = profileConfig?.UnmanagedAttributePolicy == "ENABLED";

            if (unmanagedEnabled)
            {
              return true;
            }
          }
        }        
      }

      var userProfileConfig = new UserProfileConfigRoot
      {
        Attributes = new List<UserProfileAttribute>
        {
        new UserProfileAttribute
        {
            Name = "username",
            DisplayName = "${username}",
            Multivalued = false,
            Validations = new Dictionary<string, object>
            {
                ["length"] = new { min = 3, max = 255 },
                ["username-prohibited-characters"] = new { },
                ["up-username-not-idn-homograph"] = new { }
            },
            Permissions = new UserProfilePermissions
            {
                View = new() { "admin", "user" },
                Edit = new() { "admin", "user" }
            }
        },

        new UserProfileAttribute
        {
            Name = "email",
            DisplayName = "${email}",
            Multivalued = false,
            Validations = new Dictionary<string, object>
            {
                ["email"] = new { },
                ["length"] = new { max = 255 }
            },
            Required = new UserProfileRequired
            {
                Roles = new() { "user" }
            },
            Permissions = new UserProfilePermissions
            {
                View = new() { "admin", "user" },
                Edit = new() { "admin", "user" }
            }
        },

        new UserProfileAttribute
        {
            Name = "firstName",
            DisplayName = "${firstName}",
            Multivalued = false,
            Validations = new Dictionary<string, object>
            {
                ["length"] = new { max = 255 },
                ["person-name-prohibited-characters"] = new { },
            },
            Required = new UserProfileRequired
            {
                Roles = new() { "user" }
            },
            Permissions = new UserProfilePermissions
            {
                View = new() { "admin", "user" },
                Edit = new() { "admin", "user" }
            }
        },

        new UserProfileAttribute
        {
            Name = "lastName",
            DisplayName = "${lastName}",
            Multivalued = false,
            Validations = new Dictionary<string, object>
            {
                ["length"] = new { max = 255 },
                ["person-name-prohibited-characters"] = new { },
            },
            Required = new UserProfileRequired
            {
                Roles = new() { "user" }
            },
            Permissions = new UserProfilePermissions
            {
                View = new() { "admin", "user" },
                Edit = new() { "admin", "user" }
            }
        }
    },

        Groups = new List<UserProfileGroup>
        {
            new UserProfileGroup
            {
                Name = "user-metadata",
                DisplayHeader = "User metadata",
                DisplayDescription = "Attributes, which refer to user metadata"
            }
        },

          UnmanagedAttributePolicy = "ENABLED"
        };

      var profileJson = JsonSerializer.Serialize(
    userProfileConfig,
    new JsonSerializerOptions
    {
      WriteIndented = false
    });


      var cmp = new ComponentDynamicConfig
      {
        Name = "user-profile",
        ProviderId = "declarative-user-profile",
        ProviderType = "org.keycloak.userprofile.UserProfileProvider",
        ParentId = existing?.Id!,
        Config = new Dictionary<string, IEnumerable<string>>
        {
          ["kc.user.profile.config"] = new[] { profileJson }
        }
      };

      if (component !=null )
      {
        component.Config = new Dictionary<string, IEnumerable<string>>
        {
          ["kc.user.profile.config"] = new[] { profileJson }
        };
        return await _client.UpdateComponentAsync(realm, component.Id, component);
      }
      var retval = await _client.CreateComponentAsync(realm, cmp);

      return retval;
    }
    public async Task<bool> AddRealmToCustomerAsync(string realmName, string customerUserName, string customerRealmName)
    {
      var users = await _client.GetUsersAsync(customerRealmName, username: customerUserName);
      var user = users?.FirstOrDefault();
      if (user == null) return false;

      if (user.Attributes == null)
        user.Attributes = new Dictionary<string, IEnumerable<string>>();

      if (!user.Attributes.TryGetValue("realmsOwned", out var realms))
        realms = Array.Empty<string>();

      var updated = realms.ToList();
      if (!updated.Contains(realmName))
        updated.Add(realmName);

      user.Attributes["realmsOwned"] = updated;

      return await _client.UpdateUserAsync(customerRealmName, user.Id, user);
    }

    // Создание realm
    public async Task<bool> CreateRealmAsync(string realmName)
    {
      if (await IsRealmExistAsync(realmName))
      {
        return false;
      }

      var realm = new Realm
      {
        Id = realmName,
        _Realm = realmName,
        Enabled = true,
        DefaultRoles = new[] { "offline_access", "uma_authorization", "admin", "anon", "user", "default-roles-" + realmName }
      };

      var imported = await _client.ImportRealmAsync(realmName, realm);
      if (!imported)
        return false;

      //var user_created = await CreateUserAsync(realmName, "myuser", "myuser");
      //if (!user_created)
      //  return false;

      var pubClient = new Client
      {
        ClientId = "pubclient",
        Name = "pubclient",
        Enabled = true,
        PublicClient = true,
        Protocol = "openid-connect",
        RedirectUris = new[] { "*" },
        WebOrigins = new object[] { "*" },
        StandardFlowEnabled = true,
        DirectAccessGrantsEnabled = true,
        ServiceAccountsEnabled = false,
        FrontChannelLogout = true,
        FullScopeAllowed = true,

        Attributes = new Dictionary<string, object>
        {
            { "post.logout.redirect.uris", "*" },
            { "use.refresh.tokens", "true" },
            { "oauth2.device.authorization.grant.enabled", "false" },
            { "client_credentials.use_refresh_token", "false" },
            { "require.pushed.authorization.requests", "false" },
        },

        DefaultClientScopes = new[]
        {
            "web-origins", "acr", "profile", "roles", "basic", "email"
        },

        OptionalClientScopes = new[]
          {
            "address", "phone", "offline_access", "microprofile-jwt"
          },

        ProtocolMappers = new[]
          {
        // 🔸 Включает realm roles в access_token
        new ClientProtocolMapper
        {
            Name = "realm roles",
            Protocol = "openid-connect",
            ProtocolMapper = "oidc-usermodel-realm-role-mapper",
            ConsentRequired = false,
            Config = new Dictionary<string, string>
            {
                { "multivalued", "true" },
                { "userinfo.token.claim", "true" },
                { "id.token.claim", "true" },
                { "access.token.claim", "true" },
                { "claim.name", "realm_access.roles" },
                { "jsonType.label", "String" }
            }
        },

        // 🔸 Включает client roles в access_token
        new ClientProtocolMapper
        {
            Name = "client roles",
            Protocol = "openid-connect",
            ProtocolMapper = "oidc-usermodel-client-role-mapper",
            ConsentRequired = false,
            Config = new Dictionary<string, string>
            {
                { "multivalued", "true" },
                { "userinfo.token.claim", "true" },
                { "id.token.claim", "true" },
                { "access.token.claim", "true" },
                { "claim.name", "resource_access.${client_id}.roles" },
                { "jsonType.label", "String" }
            }
        },

        // 🔸 Добавляет username
        new ClientProtocolMapper
        {
            Name = "username",
            Protocol = "openid-connect",
            ProtocolMapper = "oidc-usermodel-property-mapper",
            ConsentRequired = false,
            Config = new Dictionary<string, string>
            {
                { "userinfo.token.claim", "true" },
                { "id.token.claim", "true" },
                { "access.token.claim", "true" },
                { "claim.name", "preferred_username" },
                { "jsonType.label", "String" },
                { "user.attribute", "username" }
            }
        }
    }
      };


      return await _client.CreateClientAsync(realmName, pubClient);
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
      catch (Exception)
      {

      }


      role = new Role { Name = roleName };
      return await _client.CreateRoleAsync(realmName, role);
    }

    public async Task<bool> CreateUserAsync(string realmName, string username, string password, string email)
    {
      var users = await _client.GetUsersAsync(realmName, username: username);

      var user = users?.Where(u => u.UserName == username).FirstOrDefault();
      if (user != null) return false;

      user = new User
      {
        UserName = username,
        FirstName = username,
        LastName = username,
        Enabled = true,
        Credentials = new[]
          {
                    new Credentials { Type = "password", Value = password, Temporary = false }
          },
        EmailVerified = true,
        Email = string.IsNullOrEmpty(email) ? $"{username}@example.com" : email
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

    public async Task<IEnumerable<Role>> GetUserRolesAsync(string realmName, string userName)
    {
      var users = await _client.GetUsersAsync(realmName, username: userName);

      var user = users?.Where(u => u.UserName == userName).FirstOrDefault();
      if (user == null)
        return Enumerable.Empty<Role>();

      var roles = await _client.GetRealmRoleMappingsForUserAsync(realmName, user.Id);
      return roles ?? Enumerable.Empty<Role>();
    }

    public async Task<bool> DeleteUserAsync(string realmName, string userName)
    {
      try
      {
        var users = await _client.GetUsersAsync(realmName, username: userName);

        var user = users?.Where(u => u.UserName == userName).FirstOrDefault();
        if (user == null)
          return false;

        await _client.DeleteUserAsync(realmName, user.Id);
        return true;
      }
      catch (Exception)
      {
        return false;
      }
    }

    public async Task<RsaSecurityKey?> GetRealmPublicKeyAsync(string realm, string? kid = null)
    {
      // Получаем OpenID конфигурацию
      var oidcConfig = await _client.GetOpenIDConfigurationAsync(realm);

      using var http = new HttpClient();
      var jwksJson = await http.GetStringAsync(oidcConfig.JwksUri);
      var jwks = System.Text.Json.JsonSerializer.Deserialize<JsonWebKeySet>(jwksJson);

      if (jwks == null || jwks.Keys == null || !jwks.Keys.Any())
        return null;

      // Ищем ключ по kid или берём первый
      var key = kid != null
          ? jwks.Keys.FirstOrDefault(k => k.Kid == kid)
          : jwks.Keys.First();

      if (key == null) return null;

      return new RsaSecurityKey(new System.Security.Cryptography.RSAParameters
      {
        Modulus = Base64UrlEncoder.DecodeBytes(key.N),
        Exponent = Base64UrlEncoder.DecodeBytes(key.E)
      });
    }

    public async Task<Token> GetTokenAsync(string realm, string clientId, string username, string password)
    {
      // Получаем токен от Keycloak через готовый метод
      var token = await _client.GetTokenWithResourceOwnerPasswordCredentialsAsync(
          realm,
          clientId,
          username,
          password,
          string.Empty
      );

      return token;
    }

    public async Task<bool> IsRealmExistAsync(string realmName)
    {
      Realm? existing = null;

      try
      {
        existing = await _client.GetRealmAsync(realmName);

        if (existing != null)
        {
          return true;
        }
      }
      catch
      {

      }
      return false;
    }

    public async Task<bool> IsTokenValid(string token)
    {
      if (string.IsNullOrEmpty(token))
        return false;

      var handler = new JwtSecurityTokenHandler();
      try
      {
        var jwt = handler.ReadJwtToken(token);
        var exp = jwt.ValidTo; // UTC
        return exp > DateTime.UtcNow;
      }
      catch
      {
        return false; // токен невалидный
      }
    }
  }
}
