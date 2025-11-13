using KeycloackAdmin;
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

    [HttpPost("login_http")]
    public async Task<IActionResult> Login1([FromBody] LoginDto dto)
    {
      var client_id = "pubclient";
      if (dto.Realm == "master")
      {
        client_id = "admin-cli";
      }
      var form = new Dictionary<string, string>
            {
                { "grant_type", "password" },
                { "client_id", client_id },
                { "username", dto.Username },
                { "password", dto.Password }
            };

      // Используем базовый URL из конфигурации + имя реалма из запроса
      var url = $"{_config.Url}/realms/{dto.Realm}/protocol/openid-connect/token";

      var response = await _http.PostAsync(url, new FormUrlEncodedContent(form));
      var content = await response.Content.ReadAsStringAsync();

      if (!response.IsSuccessStatusCode)
      {
        return StatusCode((int)response.StatusCode, content);
      }

      // Можно сразу вернуть JSON токена
      return Content(content, "application/json");
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
  }
}
