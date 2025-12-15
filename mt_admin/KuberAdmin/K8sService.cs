using k8s;
using k8s.Models;

namespace KuberAdmin
{
  public class K8sService : IK8sService
  {
    private readonly IKubernetes _client;

    public K8sService()
    {
      KubernetesClientConfiguration config;

      // Если мы внутри Kubernetes — используем InClusterConfig
      if (KubernetesClientConfiguration.IsInCluster())
      {
        config = KubernetesClientConfiguration.InClusterConfig();
      }
      else
      {
        // Иначе — используем локальный kubeconfig
        var kubeConfigPath = Environment.GetEnvironmentVariable("KUBECONFIG")
                          ?? Path.Combine(
                                 Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                                 ".kube",
                                 "config");

        config = KubernetesClientConfiguration.BuildConfigFromConfigFile(kubeConfigPath);
      }

      _client = new Kubernetes(config);
    }

    public async Task<string> CreateNamespaceAsync(string name)
    {
      var ns = new V1Namespace
      {
        Metadata = new V1ObjectMeta { Name = name }
      };

      await _client.CoreV1.CreateNamespaceAsync(ns);

      return $"Namespace '{name}' created.";
    }

    public async Task<string> DeleteNamespaceAsync(string name)
    {
      await _client.CoreV1.DeleteNamespaceAsync(name);
      return $"Namespace '{name}' deleted.";
    }

    public async Task<string> DeployTenantAsync(string ns, string realmName)
    {
      var deployment = new V1Deployment
      {
        Metadata = new V1ObjectMeta
        {
          Name = "tenant-app",
          NamespaceProperty = ns
        },
        Spec = new V1DeploymentSpec
        {
          Replicas = 1,
          Selector = new V1LabelSelector
          {
            MatchLabels = new Dictionary<string, string> { { "app", "tenant-app" } }
          },
          Template = new V1PodTemplateSpec
          {
            Metadata = new V1ObjectMeta
            {
              Labels = new Dictionary<string, string> { { "app", "tenant-app" } }
            },
            Spec = new V1PodSpec
            {
              Containers = new[]
                      {
                                new V1Container
                                {
                                    Name = "tenant-app",
                                    Image = "your-registry/tenant:latest",
                                    Env = new List<V1EnvVar>
                                    {
                                      new V1EnvVar(){ Name = "REALM_NAME", Value = realmName }
                                    },
                                    Ports = new[]
                                    {
                                        new V1ContainerPort { ContainerPort = 80 }
                                    }
                                }
                            }
            }
          }
        }
      };

      await _client.AppsV1.CreateNamespacedDeploymentAsync(deployment, ns);

      return $"Deployment deployed to namespace '{ns}'.";
    }
  }
}
