using Microsoft.Azure.Cosmos;
using CosmosApp.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Cosmos DB
builder.Services.AddSingleton<CosmosClient>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var connectionString = configuration["CosmosDb:ConnectionString"];
    var primaryKey = configuration["CosmosDb:PrimaryKey"];
    
    var cosmosClientOptions = new CosmosClientOptions
    {
        ApplicationName = "CosmosApp",
        ConnectionMode = ConnectionMode.Direct,
        MaxRetryAttemptsOnRateLimitedRequests = 3,
        MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(30),
        Serializer = new CosmosSystemTextJsonSerializer(new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        })
    };

    // If connectionString contains AccountEndpoint (full connection string), parse it
    if (connectionString.Contains("AccountEndpoint="))
    {
        return new CosmosClient(connectionString, cosmosClientOptions);
    }
    // Use endpoint and key separately (for emulator or when separated)
    else if (!string.IsNullOrEmpty(primaryKey))
    {
        return new CosmosClient(connectionString, primaryKey, cosmosClientOptions);
    }
    else
    {
        return new CosmosClient(connectionString, cosmosClientOptions);
    }
});

builder.Services.AddScoped<ICosmosDbService, CosmosDbService>();
builder.Services.AddScoped<IEmployeeCosmosDbService, EmployeeCosmosDbService>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Initialize Cosmos DB
using (var scope = app.Services.CreateScope())
{
    var cosmosClient = scope.ServiceProvider.GetRequiredService<CosmosClient>();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        var databaseName = configuration["CosmosDb:DatabaseName"];
        var containerName = configuration["CosmosDb:ContainerName"];
        
        // Create database if it doesn't exist
        var database = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseName);
        logger.LogInformation("Database '{DatabaseName}' ready", databaseName);
        
        // Create container if it doesn't exist with userId as partition key
        var containerProperties = new ContainerProperties(containerName, "/userId");
        var container = await database.Database.CreateContainerIfNotExistsAsync(containerProperties);
        logger.LogInformation("Container '{ContainerName}' ready with partition key '/userId'", containerName);
        
        // Create Employee container if it doesn't exist with department as partition key
        var employeeContainerName = configuration["CosmosDb:EmployeeContainerName"];
        var employeeContainerProperties = new ContainerProperties(employeeContainerName, "/department");
        var employeeContainer = await database.Database.CreateContainerIfNotExistsAsync(employeeContainerProperties);
        logger.LogInformation("Container '{EmployeeContainerName}' ready with partition key '/department'", employeeContainerName);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to initialize Cosmos DB");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Enable static file serving
app.UseStaticFiles();

// Configure default route to serve index.html
app.UseRouting();
app.MapGet("/", () => Results.Redirect("/index.html"));

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();