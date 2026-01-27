using Testcontainers.CosmosDb;

namespace BasketService.Api.IntegrationTests;

[SetUpFixture]
public sealed class CosmosDbFixture
{
    private static CosmosDbContainer? _cosmos;

    public static string ConnectionString =>
        _cosmos?.GetConnectionString()
        ?? throw new InvalidOperationException("Cosmos DB container has not been initialized.");

    [OneTimeSetUp]
    public async Task GlobalSetup()
    {
        _cosmos = new CosmosDbBuilder("mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator")
            .WithName("cosmos-test")
            .Build();

        await _cosmos.StartAsync();
    }

    [OneTimeTearDown]
    public async Task GlobalTeardown()
    {
        if (_cosmos is not null)
            await _cosmos.DisposeAsync();
    }
}