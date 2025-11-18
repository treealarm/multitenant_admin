using KeycloackAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static System.Net.WebRequestMethods;

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
        var client_id = "pubclient";
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
    [HttpGet("ValidateToken")]
    [AllowAnonymous]
    public async Task<IActionResult> ValidateToken([FromHeader(Name = "Authorization")] string authHeader)
    {
      if (string.IsNullOrEmpty(authHeader)) return Unauthorized();

      var token = authHeader.Replace("Bearer ", "");
      var valid = await _kcAdmin.IsTokenValid(token); // метод проверки токена через Keycloak или внутреннюю логику
      if (!valid) return Unauthorized();

      return Ok();
    }

  }
}
