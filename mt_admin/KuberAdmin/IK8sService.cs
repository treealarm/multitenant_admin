using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuberAdmin
{
  public interface IK8sService
  {
    Task<string> DeleteNamespaceAsync(string name);
    Task<bool> ApplyYamlFolderAsync(string folderPath, string ns, string db_realmName);
  }

}
