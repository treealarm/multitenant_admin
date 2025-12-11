using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuberAdmin
{
  public interface IK8sService
  {
    Task<string> CreateNamespaceAsync(string name);
    Task<string> DeployTenantAsync(string ns, string realmName);
    Task<string> DeleteNamespaceAsync(string name);
  }

}
