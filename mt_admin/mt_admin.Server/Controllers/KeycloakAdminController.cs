using KeycloackAdmin;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace MultitenantAdmin.Controllers
{
  [ApiController]
  [Route("api/[controller]")]
  public class KeycloakAdminController : ControllerBase
  {
    private readonly IKeycloakAdminClient _kcAdmin;

    public KeycloakAdminController(IKeycloakAdminClient kcAdmin)
    {
      _kcAdmin = kcAdmin;
    }

    /// <summary>
    /// Создать новый Realm
    /// </summary>
    [HttpPost("realm/{realmName}")]
    public async Task<IActionResult> CreateRealm(string realmName)
    {
      var success = await _kcAdmin.CreateRealmAsync(realmName);
      if (!success) return Conflict($"Realm '{realmName}' уже существует.");
      return Ok($"Realm '{realmName}' создан.");
    }

    /// <summary>
    /// Создать нового пользователя
    /// </summary>
    [HttpPost("{realmName}/user")]
    public async Task<IActionResult> CreateUser(string realmName, [FromBody] CreateUserDto dto)
    {
      var success = await _kcAdmin.CreateUserAsync(realmName, dto.Username, dto.Password);
      if (!success) return Conflict($"Пользователь '{dto.Username}' уже существует в realm '{realmName}'.");
      return Ok($"Пользователь '{dto.Username}' создан в realm '{realmName}'.");
    }

    /// <summary>
    /// Создать новую роль
    /// </summary>
    [HttpPost("{realmName}/role/{roleName}")]
    public async Task<IActionResult> CreateRole(string realmName, string roleName)
    {
      var success = await _kcAdmin.CreateRoleAsync(realmName, roleName);
      if (!success) return Conflict($"Роль '{roleName}' уже существует в realm '{realmName}'.");
      return Ok($"Роль '{roleName}' создана в realm '{realmName}'.");
    }
  }

  /// <summary>
  /// DTO для создания пользователя
  /// </summary>
  public class CreateUserDto
  {
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
  }
}
