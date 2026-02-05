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
    private readonly IDBProvisioningService _dbAdmin;
    public DBController(IDBProvisioningService db_admin)
    {
      _dbAdmin = db_admin;
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

      await _dbAdmin.CreateDbAsync(dbName);
      return Ok($"Db {dbName} initialized");
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

      await _dbAdmin.DropDatabaseAsync(dbName);
      return Ok($"Db {dbName} dropped.");
    }
  }
}
