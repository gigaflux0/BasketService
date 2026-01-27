using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace BasketService.Api.IntegrationTests;

public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _cosmosConnection;

    public ApiFactory(string cosmosConnection)
    {
        _cosmosConnection = cosmosConnection;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            var appSettings = new Dictionary<string, string?>
            {
                ["Cosmos:ConnectionString"] = _cosmosConnection
            };

            config.AddInMemoryCollection(appSettings);
        });
    }
}
