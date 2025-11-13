using Swashbuckle.AspNetCore.Filters;

namespace mt_admin
{
  public record LoginDto
  {
    public string Realm { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
  }

  public class LoginDtoExample : IExamplesProvider<LoginDto>
  {
    public LoginDto GetExamples()
    {
      return new LoginDto
      {
        Realm = "myrealm",
        Username = "myuser",
        Password = "myuser"
      };
    }
  }
}
