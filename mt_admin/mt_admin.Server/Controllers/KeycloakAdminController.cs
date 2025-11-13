using KeycloackAdmin;
using Keycloak.Net.Models.Roles;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace mt_admin
{
  public class RoleConstants
  {
    public const string admin = "admin";
    public const string user = "user";
    public const string power_user = "power_user";
  }

  [ApiController]
  [Route("api/[controller]")]
  [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
  public class KeycloakAdminController : ControllerBase
  {
    private readonly IKeycloakAdminClient _kcAdmin;

    public KeycloakAdminController(IKeycloakAdminClient kcAdmin)
    {
      _kcAdmin = kcAdmin;
    }


    [HttpGet("whoami")]
    public IActionResult WhoAmI()
    {
      var username = User.FindFirst(ClaimTypes.Name)?.Value;
      var realm = User.FindFirst("realm")?.Value;
      var roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value);

      return Ok(new
      {
        Username = username,
        Realm = realm,
        Roles = roles
      });
    }
    /// <summary>
    /// Create a new realm.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("realm/{realmName}")]
    public async Task<IActionResult> CreateRealm(string realmName)
    {
      var success = await _kcAdmin.CreateRealmAsync(realmName);
      if (!success) return Conflict($"Realm '{realmName}' already exists.");
      return Ok($"Realm '{realmName}' has been created.");
    }

    /// <summary>
    /// Create a new user.
    /// </summary>
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = RoleConstants.admin + "," + RoleConstants.power_user)]
    [HttpPost("{realmName}/user")]
    public async Task<IActionResult> CreateUser(string realmName, [FromBody] CreateUserDto dto)
    {
      var success = await _kcAdmin.CreateUserAsync(realmName, dto.Username, dto.Password);
      if (!success) return Conflict($"User '{dto.Username}' already exists in realm '{realmName}'.");
      return Ok($"User '{dto.Username}' has been created in realm '{realmName}'.");
    }

    /// <summary>
    /// Create a new role.
    /// </summary>
    [HttpPost("{realmName}/role/{roleName}")]
    public async Task<IActionResult> CreateRole(string realmName, string roleName)
    {
      var success = await _kcAdmin.CreateRoleAsync(realmName, roleName);
      if (!success) return Conflict($"Role '{roleName}' already exists in realm '{realmName}'.");
      return Ok($"Role '{roleName}' has been created in realm '{realmName}'.");
    }

    /// <summary>
    /// Assign existing roles to a user.
    /// </summary>
    [HttpPost("{realmName}/user/{username}/roles")]
    public async Task<IActionResult> AssignRolesToUser(string realmName, string username, [FromBody] AssignRolesDto dto)
    {
      if (dto == null || dto.Roles == null || !dto.Roles.Any())
        return BadRequest("No roles provided.");

      var roles = dto.Roles.Select(r => new Role { Name = r });
      var success = await _kcAdmin.AssignRolesToUserAsync(realmName, username, roles);

      if (!success)
        return NotFound($"User '{username}' not found in realm '{realmName}'.");

      return Ok($"Roles [{string.Join(", ", dto.Roles)}] have been assigned to user '{username}' in realm '{realmName}'.");
    }

    // GET: api/users/{realm}
    [HttpGet("{realm}")]
    public async Task<IActionResult> GetUsers(string realm)
    {
      var users = await _kcAdmin.GetUsersAsync(realm);
      return Ok(users);
    }

    // GET: api/users/{realm}/{userId}/roles
    [HttpGet("{realm}/{userId}/roles")]
    public async Task<IActionResult> GetUserRoles(string realm, string userId)
    {
      var roles = await _kcAdmin.GetUserRolesAsync(realm, userId);
      return Ok(roles);
    }

    /// <summary>
    /// Delete a user.
    /// </summary>
    [HttpDelete("{realmName}/user/{userId}")]
    public async Task<IActionResult> DeleteUser(string realmName, string userId)
    {
      var success = await _kcAdmin.DeleteUserAsync(realmName, userId);
      if (!success)
        return NotFound($"User with ID '{userId}' not found in realm '{realmName}'.");
      return Ok($"User with ID '{userId}' has been deleted from realm '{realmName}'.");
    }


  }


  /// <summary>
  /// DTO for creating a user.
  /// </summary>
  public class CreateUserDto
  {
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
  }

  /// <summary>
  /// DTO for assigning roles to a user.
  /// </summary>
  public class AssignRolesDto
  {
    public IEnumerable<string> Roles { get; set; } = Enumerable.Empty<string>();
  }
}
