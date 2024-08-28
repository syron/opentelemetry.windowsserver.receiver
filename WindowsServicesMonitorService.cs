using System.Diagnostics.Metrics;
using System.ServiceProcess;

namespace opentelemetry.windowsserver.receiver;

public class WindowsServicesMonitorService : BackgroundService
    {
        private readonly ILogger<WindowsServicesMonitorService> _logger;
        private readonly Meter _meter;
        private readonly Dictionary<string, ObservableGauge<int>> _serviceGauges;

        public WindowsServicesMonitorService(ILogger<WindowsServicesMonitorService> logger)
        {
            _logger = logger;
            _meter = new Meter("WindowsServicesMonitor", "1.0.0");
            _serviceGauges = new Dictionary<string, ObservableGauge<int>>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Windows Services Monitor Service is starting.");

            var services = ServiceController.GetServices();
            CreateServiceGauges();

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Windows Services Monitor running at: {time}", DateTimeOffset.Now);
                
                UpdateServiceGauges();
                
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private void CreateServiceGauges()
        {
            foreach (var service in ServiceController.GetServices())
            {
                _serviceGauges[service.ServiceName] = _meter.CreateObservableGauge(
                    $"service_{service.ServiceName.ToLower()}_status",
                    () => IsServiceRunning(service.ServiceName) ? 1 : 0,
                    description: $"Status of the {service.ServiceName} service (1 = running, 0 = stopped)"
                );
            }
        }

        private void UpdateServiceGauges()
        {
            foreach (var service in ServiceController.GetServices())
            {
                if (!_serviceGauges.ContainsKey(service.ServiceName))
                {
                    _serviceGauges[service.ServiceName] = _meter.CreateObservableGauge(
                        $"service_{service.ServiceName.ToLower()}_status",
                        () => IsServiceRunning(service.ServiceName) ? 1 : 0,
                        description: $"Status of the {service.ServiceName} service (1 = running, 0 = stopped)"
                    );
                }
            }
        }

        private bool IsServiceRunning(string serviceName)
        {
            using var service = new ServiceController(serviceName);
            return service.Status == ServiceControllerStatus.Running;
        }
    }