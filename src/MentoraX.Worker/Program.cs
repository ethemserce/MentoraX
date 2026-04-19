using MentoraX.Infrastructure.DependencyInjection;
using MentoraX.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddHostedService<Worker>();
var host = builder.Build();
host.Run();
