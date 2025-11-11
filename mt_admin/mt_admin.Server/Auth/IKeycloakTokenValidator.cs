namespace mt_admin.Server.Auth
{
  public interface IKeycloakTokenValidator
  {
    Task<TokenValidationResult?> ValidateTokenAsync(string token);
  }

  public class TokenValidationResult
  {
    public string Username { get; set; } = "";
    public string Realm { get; set; } = "";
    public List<string> Roles { get; set; } = new();
    public DateTime ExpiresAt { get; set; }
  }

}
