using opentelemetry.windowsserver.receiver;
using OpenTelemetry.Metrics;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<WindowsServicesMonitorService>();

var services = builder.Services;
services.AddHostedService<WindowsServicesMonitorService>();
                    services.AddOpenTelemetry()
                        .WithMetrics(builder => builder
                            .AddMeter("WindowsServiceMonitor")
                            .AddOtlpExporter(options =>
                            {
                                options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                                options.Endpoint = new Uri("OpenTelemetry:OtlpEndpoint");
                            }));

var host = builder.Build();
host.Run();
