namespace mt_admin
{
  public record CustomerLoginDto
  {
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
  }
  public record LoginDto
  {
    public string Realm { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
  }
}
