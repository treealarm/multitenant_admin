using KeycloackAdmin;
using Keycloak.Net.Models.Roles;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace mt_admin
{
  [ApiController]
  [Route("api/[controller]")]
  [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
  [AllowAnonymous]
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
    [HttpPost]
    [Route("CreateRealm")]
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
    [HttpPost]
    [Route("CreateUser")]
    public async Task<IActionResult> CreateUser(CreateUserDto dto)
    {
      var success = await _kcAdmin.CreateUserAsync(dto.RealmName, dto.Username, dto.Password, dto.Email);
      if (!success) 
        return 
          Conflict($"User '{dto.Username}' already exists in realm '{dto.RealmName}'.");
      return Ok($"User '{dto.Username}' has been created in realm '{dto.RealmName}'.");
    }

    /// <summary>
    /// Create a new role.
    /// </summary>
    [HttpPost]
    [Route("CreateRoleInRealm")]
    public async Task<IActionResult> CreateRole(CreateRoleDto dto)
    {
      var success = await _kcAdmin.CreateRoleAsync(dto.RealmName, dto.RoleName);
      if (!success) return Conflict($"Role '{dto.RoleName}' already exists in realm '{dto.RealmName}'.");
      return Ok($"Role '{dto.RoleName}' has been created in realm '{dto.RealmName}'.");
    }

    /// <summary>
    /// Assign existing roles to a user.
    /// </summary>
    [HttpPost]
    [Route("AssignRolesToUser")]
    public async Task<IActionResult> AssignRolesToUser(AssignRolesDto dto)
    {
      if (dto == null || dto.Roles == null || !dto.Roles.Any())
        return BadRequest("No roles provided.");

      var roles = dto.Roles.Select(r => new Role { Name = r });
      var success = await _kcAdmin.AssignRolesToUserAsync(dto.RealmName, dto.UserName, roles);

      if (!success)
        return NotFound($"User '{dto.UserName}' not found in realm '{dto.RealmName}'.");

      return Ok($"Roles [{string.Join(", ", dto.Roles)}] have been assigned to user '{dto.UserName}' in realm '{dto.RealmName}'.");
    }

    [HttpGet]
    [Route("GetUsersByRealm")]
    public async Task<IActionResult> GetUsersByRealm(string realm)
    {
      var users = await _kcAdmin.GetUsersAsync(realm);
      return Ok(users);
    }


    [HttpPost]
    [Route("GetUserRoles")]
    public async Task<IActionResult> GetUserRoles(UserDto dto)
    {
      var roles = await _kcAdmin.GetUserRolesAsync(dto.RealmName, dto.UserName);
      return Ok(roles);
    }

    /// <summary>
    /// Delete a user.
    /// </summary>
    [HttpDelete]
    [Route("DeleteUser")]
    public async Task<IActionResult> DeleteUser(UserDto dto)
    {
      var success = await _kcAdmin.DeleteUserAsync(dto.RealmName, dto.UserName);
      if (!success)
        return NotFound($"User with ID '{dto.UserName}' not found in realm '{dto.RealmName}'.");
      return Ok($"User with ID '{dto.UserName}' has been deleted from realm '{dto.RealmName}'.");
    }

    [AllowAnonymous]
    [HttpPost("Register")]
    public async Task<IActionResult> Register(RegisterUserDto dto)
    {
      

      // Проверяем, нет ли пользователя с таким email уже
      var existingUsers = await _kcAdmin.GetUsersAsync(RegisterUserDto.Realm);
      
      existingUsers = existingUsers.Where(u=>u.Email == dto.Email);

      if (existingUsers.Any())
      {
        return Conflict($"User with email '{dto.Email}' already exists.");
      }

      // Создаём пользователя
      var createSuccess = await _kcAdmin.CreateUserAsync(RegisterUserDto.Realm, dto.Username, dto.Password, dto.Email);
      if (!createSuccess)
      {
        return StatusCode(500, "Failed to create user. Perhaps the username already exists.");
      }

      // Назначаем роль (например, tenant_admin) — если используете роли
      await _kcAdmin.AssignRolesToUserAsync(
          RegisterUserDto.Realm,
          dto.Username,
          new[] { new Role { Name = "tenant_admin" } }
      );

      return Ok($"User '{dto.Username}' has been registered and assigned 'tenant_admin' role.");
    }


  }

}
