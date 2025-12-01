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
    public async Task<IActionResult> CreateDB([FromBody] string? dbName)
    {
      if (string.IsNullOrWhiteSpace(dbName))
      {
        var me = GetWhoIAm();
        dbName = me.RealmName;
      }     

      await _provisioning.CreateDbAsync(dbName);
      return Ok($"Realm {dbName} initialized");
    }
    [HttpPost("DropDB")]
    [AllowAnonymous]
    public async Task<IActionResult> DropDB([FromBody] string? dbName = null)
    {
      if (string.IsNullOrWhiteSpace(dbName))
      {
        if (string.IsNullOrWhiteSpace(dbName))
        {
          var me = GetWhoIAm();
          dbName = me.RealmName;
        }
        if (string.IsNullOrWhiteSpace(dbName))
          return BadRequest("Realm name not provided and not found in token.");
      }

      await _provisioning.DropDatabaseAsync(dbName);
      return Ok($"Realm {dbName} dropped.");
    }
  }
}
