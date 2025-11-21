using KeycloackAdmin;

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

    private async Task DoWorkAsync(CancellationToken token)
    {
      var curDate = DateTime.UtcNow;

      while (!token.IsCancellationRequested)
      {
        await Task.Delay(1000, token);

        if (DateTime.UtcNow - curDate > TimeSpan.FromMinutes(1))
        {
          curDate = DateTime.UtcNow;
          _logger.LogInformation("GC Collect");
          GC.Collect();
        }

        // Инициализация сервисов в скоупе
        using (var scope = _serviceProvider.CreateScope())
        {
          var kcAdmin = scope.ServiceProvider.GetRequiredService<IKeycloakAdminClient>();
          if (await kcAdmin.IsRealmExistAsync(Constants.CustomerRealm)
            && await kcAdmin.EnableRealmUnmanagetAttribute(Constants.CustomerRealm)
            && _cts != null)
          {
            await _cts.CancelAsync();
          }
          else
          {
            if(await kcAdmin.CreateRealmAsync(Constants.CustomerRealm))
            {
              await kcAdmin.CreateUserAsync(Constants.CustomerRealm, "myuser", "myuser", string.Empty);
            }
          }
        }
      }
    }

    public void Dispose()
    {
      _cts?.Dispose();
    }
  }
}