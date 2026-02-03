using k8s;
using k8s.Autorest;
using k8s.Models;
using System.Net;
using YamlDotNet.Serialization;

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

    public async Task ApplyYamlAsync(List<object> objs, string ns_in)
    {
      //var objs = KubernetesYaml.LoadAllFromString(yaml);

      foreach (var obj in objs)
      {
        switch (obj)
        {
          case V1Namespace ns:
            ns.Metadata.Name = ns_in;
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
    async Task ApplyCustomYamlAsync(string yaml, string ns, string realmName)
    {
      var deserializer = new DeserializerBuilder().Build();

      // поддержка multi-doc YAML (---)
      var docs = new StringReader(yaml)
          .ReadToEnd()
          .Split("\n---", StringSplitOptions.RemoveEmptyEntries);

      foreach (var doc in docs)
      {
        var obj = deserializer.Deserialize<Dictionary<string, object>>(doc);

        var apiVersion = obj["apiVersion"].ToString();
        var kind = obj["kind"].ToString();

        var metadata = obj["metadata"] as Dictionary<object, object>;
        if (metadata != null)
        {
          metadata["namespace"] = ns;
        }

        // ConfigMap внутри CRD? (на будущее)
        if (kind == "ConfigMap" && obj.TryGetValue("data", out var dataObj))
        {
          var data = (Dictionary<string, object>)dataObj;
          data["DB_REALM_NAME"] = realmName;
        }

        if (apiVersion.StartsWith("dapr.io/"))
        {
          await ApplyDaprComponent(obj, apiVersion, kind, ns);
        }
        else
        {
          throw new NotSupportedException(
              $"Unknown custom resource {apiVersion}/{kind}");
        }
      }
    }

    async Task ApplyDaprComponent(
      Dictionary<string, object> obj,
      string apiVersion,
      string kind,
      string ns)
    {
      var parts = apiVersion.Split('/');
      var group = parts[0];        // dapr.io
      var version = parts[1];      // v1alpha1

      var plural = kind.ToLower() + "s"; // component → components

      try
      {
        await _client.CustomObjects.CreateNamespacedCustomObjectAsync(
            body: obj,
            group: group,
            version: version,
            namespaceParameter: ns,
            plural: plural
        );

      }
      catch (HttpOperationException ex)
          when (ex.Response.StatusCode == HttpStatusCode.Conflict)
      {
        // TODO: Replace (PATCH) если понадобится
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

  public async Task ApplyYamlFolderAsync(string folderPath, string ns, string realmName)
  {
    var files = Directory.GetFiles(folderPath, "*.yaml", SearchOption.AllDirectories);

    foreach (var file in files)
    {
      var yamlContent = await File.ReadAllTextAsync(file);

      List<object>? objs = null;
      try
      {
        objs = KubernetesYaml.LoadAllFromString(yamlContent);
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.ToString());
      }

      if (objs == null)
      {
        await ApplyCustomYamlAsync(yamlContent, ns, realmName);
        continue;
      }

      foreach (var obj in objs)
      {
          // 1. Проставляем namespace для всех объектов
          if (obj is IKubernetesObject<V1ObjectMeta> kObj)
          {
            kObj.Metadata.NamespaceProperty = ns;
          }

          // 2. Если ConfigMap — меняем DB_REALM_NAME
          if (obj is V1ConfigMap cm)
        {
          if (cm.Data == null)
            cm.Data = new Dictionary<string, string>();

          cm.Data["DB_REALM_NAME"] = realmName;
        }

        // 3. Применяем объект через ApplyYamlAsync
        await ApplyYamlAsync(objs, ns);
      }
    }
  }

}
}
