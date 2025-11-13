using KeycloackAdmin;
using Microsoft.AspNetCore.Mvc;

namespace mt_admin
{
  [ApiController]
  [Route("api/[controller]")]
  public class AuthController : ControllerBase
  {
    private readonly IKeycloakAdminClient _kcAdmin;

    public AuthController(IKeycloakAdminClient kcAdmin)
    {
      _kcAdmin = kcAdmin;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
      try
      {
        var content = await _kcAdmin.GetTokenAsync(dto.Realm, "pubclient", dto.Username, dto.Password);
        return Ok(content);
      }
      catch (Exception ex)
      {
        return StatusCode(500, ex.Message);
      }
    }
  }
}
