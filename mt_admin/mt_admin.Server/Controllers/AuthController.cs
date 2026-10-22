using KeycloakAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;

namespace mt_admin
{
  [ApiController]
  [Route("api/[controller]")]
  public class AuthController : ControllerBase
  {
    private readonly IKeycloakAdminClient _kcAdmin;
    private readonly HttpClient _http;
    private readonly KeycloakConfig _config;
    private readonly StandaloneMode? _standalone;

    public AuthController(IKeycloakAdminClient kcAdmin, HttpClient http, KeycloakConfig config, StandaloneMode? standalone = null)
    {
      _kcAdmin = kcAdmin;
      _http = http;
      _config = config;
      _standalone = standalone;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
      if (_standalone is not null)
        return await StandaloneLogin(_standalone, dto.Username, dto.Password);
      try
      {
        var client_id = Constants.PubClient;
        if (dto.Realm=="master")
        {
          client_id = "admin-cli";
        }
        var content = await _kcAdmin.GetTokenAsync(dto.Realm, client_id, dto.Username, dto.Password);
        return Ok(content);
      }
      catch (Exception ex)
      {
        return StatusCode(500, ex.Message);
      }
    }

    // The realm is the installation's, whatever the request says, and only its administrators get
    // in — after they have replaced their initial password in TreeAlarm VMS, which is where that is
    // done: this console is not the place to finish setting up an account.
    private async Task<IActionResult> StandaloneLogin(StandaloneMode mode, string username, string password)
    {
      Keycloak.Net.Core.Models.Root.Token token;
      try
      {
        token = await _kcAdmin.GetTokenAsync(mode.Realm, mode.ClientId, username, password);
      }
      catch (Exception)
      {
        return Unauthorized(new { code = "invalid_credentials", error = "Invalid username or password" });
      }

      var roles = RealmRoles(token.AccessToken);
      if (!roles.Contains(RoleConstants.admin))
        return StatusCode(StatusCodes.Status403Forbidden, new { code = "not_admin", error = "Only administrators manage users" });
      return Ok(token);
    }

    private static HashSet<string> RealmRoles(string accessToken)
    {
      var claim = new JwtSecurityTokenHandler().ReadJwtToken(accessToken).Claims.FirstOrDefault(c => c.Type == "realm_access")?.Value;
      if (claim is null) return [];
      using var doc = JsonDocument.Parse(claim);
      return doc.RootElement.TryGetProperty("roles", out var roles)
        ? roles.EnumerateArray().Select(r => r.GetString() ?? string.Empty).ToHashSet()
        : [];
    }

    [HttpPost("customer_login")]
    public async Task<IActionResult> CustomerLogin([FromBody] CustomerLoginDto dto)
    {
      try
      {
        var client_id = Constants.PubClient;
        var content = await _kcAdmin.GetTokenAsync(Constants.CustomerRealm, client_id, dto.Username, dto.Password);
        return Ok(content);
      }
      catch (Exception ex)
      {
        return StatusCode(500, ex.Message);
      }
    }
    [HttpGet("ValidateToken")]
    [AllowAnonymous]
    public async Task<IActionResult> ValidateToken([FromHeader(Name = "Authorization")] string authHeader)
    {
      await Task.Delay(0);
      if (string.IsNullOrEmpty(authHeader)) 
        return Unauthorized();

      var token = authHeader.Replace("Bearer ", "");
      var valid = _kcAdmin.IsTokenValid(token); // метод проверки токена через Keycloak или внутреннюю логику
      if (!valid) 
        return Unauthorized();

      return Ok();
    }

    [HttpPost("RefreshToken")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest req)
    {
      await Task.Delay(0);

      if (string.IsNullOrEmpty(req.refresh_token))
        return Unauthorized("No refresh token");

      // Проверка refresh токена
      var newAccessToken = await _kcAdmin.RefreshAccessToken(
        _standalone?.Realm ?? Constants.CustomerRealm,
        _standalone?.ClientId ?? Constants.PubClient,
        req.refresh_token);

      if (newAccessToken == null)
        return Unauthorized("Refresh token invalid");

      return Ok(newAccessToken);
    }

  }
}
