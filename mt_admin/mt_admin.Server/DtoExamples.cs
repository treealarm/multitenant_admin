using Swashbuckle.AspNetCore.Filters;

namespace mt_admin
{
  public class LoginDtoExample : IExamplesProvider<LoginDto>
  {
    public LoginDto GetExamples()
    {
      return new LoginDto
      {
        Realm = "customers",
        Username = "myuser",
        Password = "myuser"
      };
    }
  }

  public class CustomerLoginDtoExample : IExamplesProvider<CustomerLoginDto>
  {
    public CustomerLoginDto GetExamples()
    {
      return new CustomerLoginDto
      {
        Username = "myuser",
        Password = "myuser"
      };
    }
  }
}
