using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace mt_admin;

/// <summary>What the SPA should be: the multi-tenant console, or one realm's user list.</summary>
[ApiController]
[Route("api/config")]
[AllowAnonymous]
public sealed class ConfigController(StandaloneMode? standalone = null) : ControllerBase
{
  [HttpGet]
  public IActionResult Get() => Ok(new { standalone = standalone is not null, realm = standalone?.Realm });
}
