namespace mt_admin
{
  public class RegisterUserDto
  {
    public const string Realm = "customers"; // Реалм арендаторов
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
  }
}
