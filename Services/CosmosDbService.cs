using Microsoft.Azure.Cosmos;
using CosmosApp.Models;
using System.Net;

namespace CosmosApp.Services
{
    public class CosmosDbService : ICosmosDbService
    {
        private readonly Container _container;
        private readonly ILogger<CosmosDbService> _logger;

        public CosmosDbService(CosmosClient cosmosClient, IConfiguration configuration, ILogger<CosmosDbService> logger)
        {
            var databaseName = configuration["CosmosDb:DatabaseName"];
            var containerName = configuration["CosmosDb:ContainerName"];
            _container = cosmosClient.GetContainer(databaseName, containerName);
            _logger = logger;
        }

        public async Task<Item> CreateItemAsync(Item item)
        {
            try
            {
                var response = await _container.CreateItemAsync(item, new PartitionKey(item.UserId));
                
                // Log diagnostics for monitoring
                _logger.LogInformation("Item created successfully. Diagnostics: {Diagnostics}", 
                    response.Diagnostics.ToString());
                
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning("Rate limit exceeded. Retry after: {RetryAfter}ms. Diagnostics: {Diagnostics}", 
                    ex.RetryAfter?.TotalMilliseconds, ex.Diagnostics.ToString());
                throw;
            }
            catch (CosmosException ex)
            {
                _logger.LogError("Cosmos DB error: {StatusCode} - {Message}. Diagnostics: {Diagnostics}", 
                    ex.StatusCode, ex.Message, ex.Diagnostics.ToString());
                throw;
            }
        }

        public async Task<Item?> GetItemAsync(string id, string userId)
        {
            try
            {
                var response = await _container.ReadItemAsync<Item>(id, new PartitionKey(userId));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            catch (CosmosException ex)
            {
                _logger.LogError("Error retrieving item: {StatusCode} - {Message}. Diagnostics: {Diagnostics}", 
                    ex.StatusCode, ex.Message, ex.Diagnostics.ToString());
                throw;
            }
        }

        public async Task<IEnumerable<Item>> GetItemsByUserAsync(string userId)
        {
            try
            {
                var queryDefinition = new QueryDefinition(
                    "SELECT * FROM c WHERE c.userId = @userId ORDER BY c.createdAt DESC")
                    .WithParameter("@userId", userId);

                var query = _container.GetItemQueryIterator<Item>(
                    queryDefinition, 
                    requestOptions: new QueryRequestOptions
                    {
                        PartitionKey = new PartitionKey(userId),
                        MaxItemCount = 100
                    });

                var items = new List<Item>();
                while (query.HasMoreResults)
                {
                    var response = await query.ReadNextAsync();
                    items.AddRange(response.ToList());
                    
                    // Log high latency operations
                    if (response.RequestCharge > 10)
                    {
                        _logger.LogWarning("High RU consumption: {RequestCharge} RUs. Diagnostics: {Diagnostics}", 
                            response.RequestCharge, response.Diagnostics.ToString());
                    }
                }

                return items;
            }
            catch (CosmosException ex)
            {
                _logger.LogError("Error querying items: {StatusCode} - {Message}. Diagnostics: {Diagnostics}", 
                    ex.StatusCode, ex.Message, ex.Diagnostics.ToString());
                throw;
            }
        }

        public async Task<Item> UpdateItemAsync(Item item)
        {
            try
            {
                var response = await _container.UpsertItemAsync(item, new PartitionKey(item.UserId));
                return response.Resource;
            }
            catch (CosmosException ex)
            {
                _logger.LogError("Error updating item: {StatusCode} - {Message}. Diagnostics: {Diagnostics}", 
                    ex.StatusCode, ex.Message, ex.Diagnostics.ToString());
                throw;
            }
        }

        public async Task DeleteItemAsync(string id, string userId)
        {
            try
            {
                await _container.DeleteItemAsync<Item>(id, new PartitionKey(userId));
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Item already deleted or doesn't exist
                return;
            }
            catch (CosmosException ex)
            {
                _logger.LogError("Error deleting item: {StatusCode} - {Message}. Diagnostics: {Diagnostics}", 
                    ex.StatusCode, ex.Message, ex.Diagnostics.ToString());
                throw;
            }
        }
    }
}