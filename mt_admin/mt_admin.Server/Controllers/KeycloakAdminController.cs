using KeycloakAdmin;
using Keycloak.Net.Models.Roles;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mt_admin.Dto;
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
    private RolesDto GetWhoIAm()
    {
      return new RolesDto
      {
        RealmName = User.FindFirst("realm")?.Value ?? string.Empty,
        UserName = User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty,
        Roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value)
      };
    }

    [HttpGet("whoami")]
    public IActionResult WhoAmI()
    {
      var username = User.FindFirst(ClaimTypes.Name)?.Value;
      var realm = User.FindFirst("realm")?.Value;
      var roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value);

      return Ok(GetWhoIAm());
    }
    /// <summary>
    /// Create a new realm.
    /// </summary>
    [AllowAnonymous]
    [HttpPost]
    [Route("CreateRealm")]
    public async Task<IActionResult> CreateRealm(string realmName)
    {
      var me = GetWhoIAm();

      if (me.RealmName != Constants.CustomerRealm)
      {
        return Conflict($"Realm '{me.RealmName}' is not alowed to create new realm.");
      }
      if (string.IsNullOrEmpty(me.UserName))
      {
        return Conflict($"User '{me.UserName}' is empty.");
      }

      var success = await _kcAdmin.CreateRealmAsync(realmName, Constants.PubClient);
      if (!success) 
        return Conflict($"Realm '{realmName}' already exists.");

      success = await _kcAdmin.AddRealmToCustomerAsync(realmName, me.UserName, Constants.CustomerRealm);
      return Ok($"Realm '{realmName}' has been created.");
    }

    [HttpDelete]
    [Route("DeleteRealm")]
    public async Task<IActionResult> DeleteRealm(DeleteRealmDto req)
    {
      var me = GetWhoIAm();

      if (me.RealmName != Constants.CustomerRealm)
      {
        return Conflict($"Realm '{me.RealmName}' is not allowed to delete realms.");
      }

      if (string.IsNullOrEmpty(req.RealmName))
        return BadRequest("RealmName is required.");

      // удалить сам реалм
      var success = await _kcAdmin.DeleteRealmAsync(req.RealmName);
      if (!success)
        return Conflict($"Realm '{req.RealmName}' does not exist or cannot be deleted.");

      // убрать его из realmsOwned текущего пользователя
      await _kcAdmin.RemoveRealmFromCustomerAsync(req.RealmName, me.UserName, Constants.CustomerRealm);

      return Ok($"Realm '{req.RealmName}' has been deleted.");
    }


    [AllowAnonymous]
    [HttpGet]
    [Route("GetRealmComponents")]
    public async Task<IActionResult> GetRealmComponents(string realmName)
    {
      var data = await _kcAdmin.GetRealmComponents(realmName);
      return Ok(data);
    }

    [AllowAnonymous]
    [HttpGet]
    [Route("GetRealmRoles")]
    public async Task<IActionResult> GetRealmRoles(string realmName)
    {
      var data = await _kcAdmin.GetRealmRolesAsync(realmName);
      return Ok(data.Select(r=>r.Name));
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
    public async Task<IActionResult> AssignRolesToUser(RolesDto dto)
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

    [Authorize]
    [HttpGet("GetLoggedInUser")]
    public async Task<IActionResult> GetLoggedInUser()
    {
      var me = GetWhoIAm();

      if (string.IsNullOrEmpty(me.UserName))
        return Unauthorized("UserName not found in token");

      if (string.IsNullOrEmpty(me.RealmName))
        return Unauthorized("RealmName not found in token");

      // Тянем всех пользователей реалма
      var users = await _kcAdmin.GetUsersAsync(me.RealmName);
      if (users == null)
        return NotFound("Realm users not found");

      // Ищем текущего
      var user = users.FirstOrDefault(u => u.UserName == me.UserName);

      if (user == null)
        return NotFound($"User {me.UserName} not found in realm {me.RealmName}");

      return Ok(user);
    }


    [HttpPost]
    [Route("GetUserRoles")]
    public async Task<IActionResult> GetUserRoles(UserDto dto)
    {
      var roles = await _kcAdmin.GetUserRolesAsync(dto.RealmName, dto.UserName);
      return Ok(roles.Select(r=>r.Name));
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
      var existingUsers = await _kcAdmin.GetUsersAsync(Constants.CustomerRealm);
      
      existingUsers = existingUsers.Where(u=>u.Email == dto.Email);

      if (existingUsers.Any())
      {
        return Conflict($"User with email '{dto.Email}' already exists.");
      }

      // Создаём пользователя
      var createSuccess = await _kcAdmin.CreateUserAsync(Constants.CustomerRealm, dto.Username, dto.Password, dto.Email);
      if (!createSuccess)
      {
        return StatusCode(500, "Failed to create user. Perhaps the username already exists.");
      }

      // Назначаем роль (например, tenant_admin) — если используете роли
      await _kcAdmin.AssignRolesToUserAsync(
          Constants.CustomerRealm,
          dto.Username,
          new[] { new Role { Name = "tenant_admin" } }
      );

      return Ok($"User '{dto.Username}' has been registered and assigned 'tenant_admin' role.");
    }


  }

}
