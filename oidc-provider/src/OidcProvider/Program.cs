using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OidcProvider;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddSingleton<CommandProcessor>();
    })
    .Build();

await host.RunAsync();
