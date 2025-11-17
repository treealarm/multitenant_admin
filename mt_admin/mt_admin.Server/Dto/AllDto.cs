namespace mt_admin
{
  public class UserDto
  {
    public string UserName { get; set; } = string.Empty;
    public string RealmName { get; set; } = string.Empty;
  }

  public class CreateRoleDto
  {
    public string RoleName { get; set; } = string.Empty;
    public string RealmName { get; set; } = string.Empty;
  }

  public class CreateUserDto
  {
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string RealmName { get; set; } = string.Empty;
  }


  public class AssignRolesDto
  {
    public string UserName { get; set; } = string.Empty;
    public string RealmName { get; set; } = string.Empty;
    public IEnumerable<string> Roles { get; set; } = Enumerable.Empty<string>();
  }
}
