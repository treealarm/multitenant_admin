using System.Security.Claims;
using Keycloak.Net.Models.Roles;
using KeycloakAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace mt_admin;

/// <summary>
/// The users of a standalone installation's realm (StandaloneMode). Nobody is made to change the
/// password an administrator set: whoever wants another sets it in TreeAlarm VMS.
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize(Policy = StandaloneMode.AdminPolicy)]
public sealed class UsersController(IKeycloakAdminClient kc, StandaloneMode mode) : ControllerBase
{
  // TreeAlarm VMS's own rule for a new password (web_srv AuthController.MinPasswordLength): the realm
  // has no password policy, Keycloak would refuse to import the initial admin/admin under one.
  private const int MinPasswordLength = 8;

  public sealed record UserDto(string Username, bool IsAdmin);
  public sealed record NewUserDto(string Username, string Password, bool IsAdmin);
  public sealed record PasswordDto(string Password);
  public sealed record AdminDto(bool IsAdmin);

  private string Me => User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;

  [HttpGet]
  public async Task<IActionResult> List()
  {
    var result = new List<UserDto>();
    foreach (var user in (await kc.GetUsersAsync(mode.Realm)).OrderBy(u => u.UserName, StringComparer.OrdinalIgnoreCase))
    {
      var roles = await RoleNamesAsync(user.UserName);
      result.Add(new UserDto(user.UserName, roles.Contains(RoleConstants.admin)));
    }
    return Ok(result);
  }

  [HttpPost]
  public async Task<IActionResult> Create(NewUserDto dto)
  {
    var username = dto.Username.Trim();
    if (username.Length == 0)
      return BadRequest(Error("username_required", "A username is required"));
    if (PasswordProblem(dto.Password, username) is { } problem)
      return BadRequest(problem);
    // CreateUserAsync reports an existing user as success.
    if ((await kc.GetUsersAsync(mode.Realm)).Any(u => string.Equals(u.UserName, username, StringComparison.OrdinalIgnoreCase)))
      return Conflict(Error("user_exists", $"User '{username}' already exists"));

    if (!await kc.CreateUserAsync(mode.Realm, username, dto.Password, email: string.Empty))
      return StatusCode(StatusCodes.Status502BadGateway, Error("keycloak", "Keycloak did not create the user"));

    var roles = new List<string> { dto.IsAdmin ? RoleConstants.admin : RoleConstants.user };
    await SetRolesAsync(username, roles);
    return Ok();
  }

  [HttpDelete("{username}")]
  public async Task<IActionResult> Delete(string username)
  {
    if (string.Equals(username, Me, StringComparison.OrdinalIgnoreCase))
      return BadRequest(Error("self", "You cannot delete your own account"));
    return await kc.DeleteUserAsync(mode.Realm, username) ? Ok() : NotFound();
  }

  [HttpPost("{username}/password")]
  public async Task<IActionResult> ResetPassword(string username, PasswordDto dto)
  {
    if (PasswordProblem(dto.Password, username) is { } problem)
      return BadRequest(problem);
    return await kc.SetPasswordAsync(mode.Realm, username, dto.Password) ? Ok() : NotFound();
  }

  [HttpPut("{username}/admin")]
  public async Task<IActionResult> SetAdmin(string username, AdminDto dto)
  {
    if (!dto.IsAdmin && string.Equals(username, Me, StringComparison.OrdinalIgnoreCase))
      return BadRequest(Error("self", "You cannot take away your own administrator role"));

    var roles = await RoleNamesAsync(username);
    if (dto.IsAdmin) { roles.Add(RoleConstants.admin); roles.Remove(RoleConstants.user); }
    else { roles.Remove(RoleConstants.admin); roles.Add(RoleConstants.user); }
    return await SetRolesAsync(username, roles) ? Ok() : NotFound();
  }

  private async Task<HashSet<string>> RoleNamesAsync(string username) =>
    (await kc.GetUserRolesAsync(mode.Realm, username)).Select(r => r.Name).ToHashSet(StringComparer.Ordinal);

  // AssignRolesToUserAsync replaces the whole set, so callers pass everything the user keeps.
  private Task<bool> SetRolesAsync(string username, IEnumerable<string> roles) =>
    kc.AssignRolesToUserAsync(mode.Realm, username, roles.Distinct().Select(name => new Role { Name = name }));

  private static object? PasswordProblem(string password, string username) =>
    password.Length < MinPasswordLength
      ? Error("too_short", $"The password must be at least {MinPasswordLength} characters long")
      : string.Equals(password, username, StringComparison.OrdinalIgnoreCase)
        ? Error("same_as_username", "The password must differ from the username")
        : null;

  private static object Error(string code, string message) => new { code, error = message };
}
