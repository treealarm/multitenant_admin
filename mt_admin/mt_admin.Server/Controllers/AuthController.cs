using Microsoft.AspNetCore.Mvc;

namespace mt_admin.Server.Controllers
{
  [ApiController]
  [Route("api/[controller]")]
  public class AuthController : ControllerBase
  {
    private readonly HttpClient _http;
    private readonly KeycloakConfig _config;

    public AuthController(HttpClient http, KeycloakConfig config)
    {
      _http = http;
      _config = config;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
      var form = new Dictionary<string, string>
            {
                { "grant_type", "password" },
                { "client_id", _config.ClientId },
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
  }

  public record LoginDto(string Realm, string Username, string Password);

}
