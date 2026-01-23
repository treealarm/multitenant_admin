using k8s;
using k8s.Autorest;
using k8s.Models;
using System.Net;

// посмотреть айпи миникубера
// kubectl config view --minify 
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

    async Task ApplyNamespace(V1Namespace ns)
    {
      try
      {
        await _client.CoreV1.ReadNamespaceAsync(ns.Metadata.Name);
        await _client.CoreV1.ReplaceNamespaceAsync(ns, ns.Metadata.Name);
      }
      catch
      {
        await _client.CoreV1.CreateNamespaceAsync(ns);
      }
    }

    async Task ApplyDeployment(V1Deployment dep)
    {
      try
      {
        await _client.AppsV1.ReadNamespacedDeploymentAsync(
          dep.Metadata.Name,
          dep.Metadata.NamespaceProperty);

        await _client.AppsV1.ReplaceNamespacedDeploymentAsync(
          dep,
          dep.Metadata.Name,
          dep.Metadata.NamespaceProperty);
      }
      catch
      {
        await _client.AppsV1.CreateNamespacedDeploymentAsync(
          dep,
          dep.Metadata.NamespaceProperty);
      }
    }

    async Task ApplyConfigMap(V1ConfigMap cm)
    {
      var name = cm.Metadata.Name;
      var ns = cm.Metadata.NamespaceProperty;

      try
      {
        var existing = await _client.CoreV1.ReadNamespacedConfigMapAsync(name, ns);

        // сохраняем системные поля
        cm.Metadata.ResourceVersion = existing.Metadata.ResourceVersion;

        await _client.CoreV1.ReplaceNamespacedConfigMapAsync(
          cm,
          name,
          ns);
      }
      catch (HttpOperationException ex)
        when (ex.Response.StatusCode == HttpStatusCode.NotFound)
      {
        await _client.CoreV1.CreateNamespacedConfigMapAsync(cm, ns);
      }
    }

    async Task ApplySecret(V1Secret secret)
    {
      var name = secret.Metadata.Name;
      var ns = secret.Metadata.NamespaceProperty;

      try
      {
        var existing = await _client.CoreV1.ReadNamespacedSecretAsync(name, ns);

        // обязательно для replace
        secret.Metadata.ResourceVersion = existing.Metadata.ResourceVersion;

        await _client.CoreV1.ReplaceNamespacedSecretAsync(
          secret,
          name,
          ns);
      }
      catch (HttpOperationException ex)
        when (ex.Response.StatusCode == HttpStatusCode.NotFound)
      {
        await _client.CoreV1.CreateNamespacedSecretAsync(secret, ns);
      }
    }

    public async Task ApplyYamlAsync(string yaml)
    {
      var objs = KubernetesYaml.LoadAllFromString(yaml);

      foreach (var obj in objs)
      {
        switch (obj)
        {
          case V1Namespace ns:
            await ApplyNamespace(ns);
            break;

          case V1ConfigMap cm:
            await ApplyConfigMap(cm);
            break;

          case V1Secret sec:
            await ApplySecret(sec);
            break;

          case V1Deployment dep:
            await ApplyDeployment(dep);
            break;

          case V1Service svc:
            await ApplyService(svc);
            break;

          default:
            throw new NotSupportedException(
              $"Unsupported k8s object {obj.GetType().Name}");
        }
      }
    }

    async Task ApplyService(V1Service svc)
    {
      var name = svc.Metadata.Name;
      var ns = svc.Metadata.NamespaceProperty;

      try
      {
        var existing = await _client.CoreV1.ReadNamespacedServiceAsync(name, ns);

        // ОБЯЗАТЕЛЬНО:
        svc.Metadata.ResourceVersion = existing.Metadata.ResourceVersion;

        // ВАЖНО: ClusterIP immutable
        svc.Spec.ClusterIP = existing.Spec.ClusterIP;
        svc.Spec.ClusterIPs = existing.Spec.ClusterIPs;

        await _client.CoreV1.ReplaceNamespacedServiceAsync(
          svc,
          name,
          ns);
      }
      catch (HttpOperationException ex)
        when (ex.Response.StatusCode == HttpStatusCode.NotFound)
      {
        await _client.CoreV1.CreateNamespacedServiceAsync(svc, ns);
      }
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
