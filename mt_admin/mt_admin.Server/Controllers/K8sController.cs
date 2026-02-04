using KuberAdmin;
using Microsoft.AspNetCore.Mvc;

namespace mt_admin.Controllers
{
  [ApiController]
  [Route("api/[controller]")]
  public class K8sController : ControllerBase
  {
    private readonly IK8sService _k8s;

    public K8sController(IK8sService k8s)
    {
      _k8s = k8s;
    }


    [HttpPost("DeployTenant")]
    public async Task<IActionResult> DeployTenant([FromBody] DeployTenantRequest req)
    {
      try
      {
        string yamlFolder = Path.Combine(AppContext.BaseDirectory, "k8_yaml");

        await _k8s.ApplyYamlFolderAsync(yamlFolder, req.Namespace, req.RealmName);

        return Ok($"Tenant {req.RealmName} deployed to namespace {req.Namespace}");
      }
      catch (Exception ex)
      {
        return BadRequest(ex.Message);
      }
    }

    [HttpDelete("DeleteNamespace")]
    public async Task<IActionResult> DeleteNamespace([FromBody] string ns)
    {
      try
      {
        var result = await _k8s.DeleteNamespaceAsync(ns);
        return Ok(result);
      }
      catch (Exception ex)
      {
        return BadRequest(ex.Message);
      }
    }
  }

  public record DeployTenantRequest(string Namespace, string RealmName);

}
