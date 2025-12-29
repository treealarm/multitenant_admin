using KeycloakAdmin;
using Keycloak.Net.Models.Clients;
using DbAdmin;
using Keycloak.Net.Models.Roles;

namespace mt_admin
{
  public class InitHostedService : IHostedService, IDisposable
  {
    private readonly ILogger<InitHostedService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private Task? _backgroundTask;
    private CancellationTokenSource? _cts;

    public InitHostedService(
      ILogger<InitHostedService> logger,
      IServiceProvider serviceProvider)
    {
      _logger = logger;
      _serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
      _logger.LogInformation("InitHostedService is starting...");

      // Запускаем фоновую задачу
      _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
      _backgroundTask = Task.Run(() => DoWorkAsync(_cts.Token), _cts.Token);

      _logger.LogInformation("InitHostedService started.");
      return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
      _logger.LogInformation("InitHostedService is stopping...");

      _cts?.Cancel();

      return _backgroundTask ?? Task.CompletedTask;
    }

    private async Task<bool> CreateCustomerRealm(CancellationToken token)
    {
      // Инициализация сервисов в скоупе
      using (var scope = _serviceProvider.CreateScope())
      {
        var kcAdmin = scope.ServiceProvider.GetRequiredService<IKeycloakAdminClient>();
        if (await kcAdmin.IsRealmExistAsync(Constants.CustomerRealm)
          && await kcAdmin.EnableRealmUnmanagetAttribute(Constants.CustomerRealm)
          && _cts != null)
        {
          return true;
        }
        else
        {
          if (await kcAdmin.CreateRealmAsync(Constants.CustomerRealm, Constants.PubClient))
          {
            await kcAdmin.CreateUserAsync(Constants.CustomerRealm, "myuser", "myuser", string.Empty);
          }
        }
      }
      return false;
    }

    private async Task<bool> CreateMyRealm(CancellationToken token)
    {
      _logger.LogInformation("CreateMyRealm start");

      const string myrealm = "myrealm";
      const string myuser = "myuser";
      // Инициализация сервисов в скоупе
      using (var scope = _serviceProvider.CreateScope())
      {
        var kcAdmin = scope.ServiceProvider.GetRequiredService<IKeycloakAdminClient>();

        if (!await kcAdmin.CreateRealmAsync(myrealm, Constants.PubClient))
        {
          return false;
        };
        if (!await kcAdmin.CreateUserAsync(myrealm, myuser, myuser, string.Empty))
        {
          return false; 
        }

        var roles = new List<Role>() { new Role{ Name = "admin" } };
        if (!await kcAdmin.AssignRolesToUserAsync(myrealm, myuser, roles))
        {
          return false;
        }

        if (!await kcAdmin.AddRealmToCustomerAsync(myrealm, myuser, Constants.CustomerRealm))
        {
          return false;
        }
        if (!await kcAdmin.EnableRealmUnmanagetAttribute(myrealm))
        {
          return false;
        }
        var provisioning = scope.ServiceProvider.GetRequiredService<IDBProvisioningService>();
        if (!await provisioning.CreateDbAsync(myrealm))
        {
          return false;
        }
      }

      _logger.LogInformation("CreateMyRealm succ");
      return true;
    }
    private async Task DoWorkAsync(CancellationToken token)
    {
      var curDate = DateTime.UtcNow;

      while (!token.IsCancellationRequested)
      {
        _logger.LogInformation("DoWorkAsync cycle....");
        await Task.Delay(1000, token);

        if (DateTime.UtcNow - curDate > TimeSpan.FromMinutes(1))
        {
          curDate = DateTime.UtcNow;
          _logger.LogInformation("GC Collect");
          GC.Collect();
        }

        try
        {
          if (await CreateCustomerRealm(token) && await CreateMyRealm(token))
          {
            await _cts!.CancelAsync();
          }
        }
        catch (Exception ex)
        {
          _logger.LogError(ex.ToString());
        }
      }
    }

    public void Dispose()
    {
      _cts?.Dispose();
    }
  }
}