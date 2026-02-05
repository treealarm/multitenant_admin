using DbAdmin;
using KeycloakAdmin;
using KuberAdmin;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using mt_admin.Dto;
using System.Security.Claims;


namespace mt_admin
{
  public record ProvisionRequest(string RealmName);



  [ApiController]
  [Route("api/[controller]")]
  [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
  [AllowAnonymous]
  public class ProvisionController : ControllerBase
  {
    private readonly IKeycloakAdminClient _kcAdmin;
    private readonly IDBProvisioningService _dbAdmin;
    private readonly IK8sService _k8s;

    public ProvisionController(IKeycloakAdminClient kcAdmin,
      IDBProvisioningService db_admin,
      IK8sService k8s)
    {
      _kcAdmin = kcAdmin;
      _dbAdmin = db_admin;
      _k8s = k8s;
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

    [HttpPost("CreateRealm")]
    public async Task<IActionResult> CreateRealm(ProvisionRequest req)
    {
      var realmName = req.RealmName;
      if (string.IsNullOrWhiteSpace(realmName))
      {
        return Conflict($"Realm '{realmName}' is not correct for new realm.");
      }
      try
      {
        var me = GetWhoIAm();

        if (me.RealmName != Constants.CustomerRealm)
        {
          return Conflict($"Realm '{me.RealmName}' is not alowed to create new realm.");
        }
        if (string.IsNullOrEmpty(me.UserName))
        {
          return Conflict($"User '{me.UserName}' is empty.");
        }

        var success = await _kcAdmin.CreateRealmAsync(realmName, Constants.PubClient);
        if (!success)
          return Conflict($"Realm '{realmName}' already exists.");

        success = await _kcAdmin.AddRealmToCustomerAsync(realmName, me.UserName, Constants.CustomerRealm);
        if (!success)
          return Conflict($"Realm '{realmName}' is not added to {me.UserName} under {Constants.CustomerRealm}");

        // Create DB

        success = await _dbAdmin.CreateDbAsync(realmName);

        if (!success)
          return Conflict($"Db {realmName} is not initialized");

        // create k8 namespace


        string yamlFolder = Path.Combine(AppContext.BaseDirectory, "k8_yaml");

        success = await _k8s.ApplyYamlFolderAsync(yamlFolder, realmName, realmName);

        if (!success)
          return Conflict($"Tenant not deployed to namespace {realmName} with db {realmName}");
      }
      catch (Exception ex)
      {
        return BadRequest(ex.Message);
      }
      return Ok($"Realm '{realmName}' has been created.");
    }

    [HttpDelete]
    [Route("DeprovisionRealm")]
    public async Task<IActionResult> DeprovisionRealm(DeleteRealmDto req)
    {
      var me = GetWhoIAm();

      if (me.RealmName != Constants.CustomerRealm)
      {
        return Conflict($"Realm '{me.RealmName}' is not allowed to delete realms.");
      }

      if (string.IsNullOrEmpty(req.RealmName))
        return BadRequest("RealmName is required.");

      // удалить сам реалм
      var success = await _kcAdmin.DeleteRealmAsync(req.RealmName);
      if (!success)
        return Conflict($"Realm '{req.RealmName}' does not exist or cannot be deleted.");

      // убрать его из realmsOwned текущего пользователя
      await _kcAdmin.RemoveRealmFromCustomerAsync(req.RealmName, me.UserName, Constants.CustomerRealm);

      var dbName = req.RealmName;      

      if (string.IsNullOrWhiteSpace(dbName))
      {
          return BadRequest("Realm name not provided and not found in token.");
      }

      await _dbAdmin.DropDatabaseAsync(dbName);
      await _k8s.DeleteNamespaceAsync(req.RealmName);

      return Ok($"Realm '{req.RealmName}' has been deleted.");
    }
  }
}

