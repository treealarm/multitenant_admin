using DbAdmin;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace mt_admin
{
  [Route("api/[controller]")]
  [ApiController]
  [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
  [AllowAnonymous]
  public class DBController : ControllerBase
  {
    private readonly IDBProvisioningService _provisioning;
    public DBController(IDBProvisioningService provisioning)
    {
      _provisioning = provisioning;
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

    [HttpPost("CreateDB")]
    public async Task<IActionResult> CreateDB([FromBody] string realmName)
    {
      if (string.IsNullOrWhiteSpace(realmName))
      {
        var me = GetWhoIAm();
        realmName = me.RealmName;
      }     

      await _provisioning.ProvisionRealmAsync(realmName);
      return Ok($"Realm {realmName} initialized");
    }
  }
}
