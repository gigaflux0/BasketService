using Application.Ports;
using CosmosDb.EventStore;
using CosmosDb.ReadModel;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectionWorker.Ports;

namespace CosmosDb;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCosmosDb(this IServiceCollection s) => s
        .AddSingleton<IBasketRepository, CosmosBasketRepository>()
        .AddScoped<IBasketReadModelRepository, CosmosBasketReadModelRepository>()
        .AddSingleton<IEventStreamReader, CosmosBasketRepository>()
        .AddSingleton<IBasketProjectionWriter, CosmosBasketProjectionWriter>()
        .AddSingleton<IEventStreamSubscription, CosmosEventStreamSubscription>()
        .AddSingleton<Func<CosmosClient>>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var connectionString = config["Cosmos:ConnectionString"];

            return () => new CosmosClient(connectionString, new CosmosClientOptions
            {
                ConnectionMode = ConnectionMode.Gateway,
                EnableTcpConnectionEndpointRediscovery = false,
                HttpClientFactory = () =>
                {
                    var handler = new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback =
                            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    };

                    return new HttpClient(handler);
                }
            });
        });
}