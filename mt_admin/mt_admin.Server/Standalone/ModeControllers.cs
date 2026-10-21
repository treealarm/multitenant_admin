using System.Reflection;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace mt_admin;

// Which controllers exist. Standalone: login, configuration and the realm's users only — the
// multi-tenant ones would create realms, databases and Kubernetes namespaces on a machine that has
// none to spare. Multi-tenant: everything but UsersController, which serves the one fixed realm of
// a standalone installation.
internal sealed class ModeControllers(bool standalone) : ControllerFeatureProvider
{
  private static readonly HashSet<Type> StandaloneOnly = [typeof(UsersController)];
  private static readonly HashSet<Type> InStandalone = [typeof(AuthController), typeof(ConfigController), typeof(UsersController)];

  protected override bool IsController(TypeInfo typeInfo) =>
    base.IsController(typeInfo)
    && (standalone ? InStandalone.Contains(typeInfo.AsType()) : !StandaloneOnly.Contains(typeInfo.AsType()));
}
