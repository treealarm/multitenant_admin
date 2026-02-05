using KeycloakAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace mt_admin
{
  [ApiController]
  [Route("api/[controller]")]
  public class AuthController : ControllerBase
  {
    private readonly IKeycloakAdminClient _kcAdmin;
    private readonly HttpClient _http;
    private readonly KeycloakConfig _config;

    public AuthController(IKeycloakAdminClient kcAdmin, HttpClient http, KeycloakConfig config)
    {
      _kcAdmin = kcAdmin;
      _http = http;
      _config = config;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
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
        Constants.CustomerRealm, 
        Constants.PubClient, 
        req.refresh_token);

      if (newAccessToken == null)
        return Unauthorized("Refresh token invalid");

      return Ok(newAccessToken);
    }

  }
}
