# CosmosApp

ASP.NET Core Web API application with Azure Cosmos DB integration.

## Features

- RESTful API for CRUD operations
- Azure Cosmos DB SQL API integration
- Partition key optimization (userId)
- Comprehensive error handling and logging
- Swagger/OpenAPI documentation
- Request/Response DTOs with validation

## API Endpoints

### Items
- `GET /api/items/{userId}` - Get all items for a user
- `GET /api/items/{userId}/{id}` - Get specific item
- `POST /api/items` - Create new item
- `PUT /api/items/{userId}/{id}` - Update item
- `DELETE /api/items/{userId}/{id}` - Delete item

## Setup

### Using Cosmos DB Emulator (Development)

1. Install and start the [Azure Cosmos DB Emulator](https://learn.microsoft.com/azure/cosmos-db/emulator)
2. The default configuration in `appsettings.json` is already set for the emulator
3. Run the application:
   ```bash
   dotnet run
   ```

### Using Azure Cosmos DB (Production)

1. Update `appsettings.Development.json` with your Cosmos DB connection details:
   ```json
   {
     "CosmosDb": {
       "ConnectionString": "AccountEndpoint=https://your-account.documents.azure.com:443/;AccountKey=your-key;",
       "DatabaseName": "YourDatabaseName",
       "ContainerName": "YourContainerName"
     }
   }
   ```

2. Or use User Secrets for sensitive data:
   ```bash
   dotnet user-secrets set "CosmosDb:ConnectionString" "your-connection-string"
   ```

## Project Structure

```
CosmosApp/
├── Controllers/
│   └── ItemsController.cs       # API endpoints
├── DTOs/
│   └── ItemDTOs.cs             # Request/Response models
├── Models/
│   └── Item.cs                 # Cosmos DB document model
├── Services/
│   ├── ICosmosDbService.cs     # Service interface
│   └── CosmosDbService.cs      # Cosmos DB operations
└── Program.cs                  # App configuration & DI setup
```

## Cosmos DB Best Practices Implemented

- **Partition Key**: Uses `userId` for optimal query performance
- **Error Handling**: Comprehensive exception handling with diagnostic logging
- **Request Units**: Monitoring and logging of RU consumption
- **Connection Management**: Singleton CosmosClient with optimal settings
- **Query Optimization**: Partition-aware queries to minimize cross-partition operations

## Testing

Use the Swagger UI at `https://localhost:{port}/swagger` to test the API endpoints.