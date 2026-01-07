using Microsoft.Azure.Cosmos;
using CosmosApp.Models;
using System.Net;

namespace CosmosApp.Services
{
    public class EmployeeCosmosDbService : IEmployeeCosmosDbService
    {
        private readonly Container _container;
        private readonly ILogger<EmployeeCosmosDbService> _logger;

        public EmployeeCosmosDbService(CosmosClient cosmosClient, IConfiguration configuration, ILogger<EmployeeCosmosDbService> logger)
        {
            var databaseName = configuration["CosmosDb:DatabaseName"];
            var employeeContainerName = configuration["CosmosDb:EmployeeContainerName"];
            
            if (string.IsNullOrEmpty(databaseName))
                throw new ArgumentException("CosmosDb:DatabaseName configuration is missing");
            if (string.IsNullOrEmpty(employeeContainerName))
                throw new ArgumentException("CosmosDb:EmployeeContainerName configuration is missing");
                
            _container = cosmosClient.GetContainer(databaseName, employeeContainerName);
            _logger = logger;
        }

        public async Task<Employee> CreateEmployeeAsync(Employee employee)
        {
            try
            {
                var response = await _container.CreateItemAsync(employee, new PartitionKey(employee.Department));
                
                // Log diagnostics for monitoring
                _logger.LogInformation("Employee created successfully. Diagnostics: {Diagnostics}", 
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

        public async Task<Employee?> GetEmployeeAsync(string id, string department)
        {
            try
            {
                var response = await _container.ReadItemAsync<Employee>(id, new PartitionKey(department));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            catch (CosmosException ex)
            {
                _logger.LogError("Error retrieving employee: {StatusCode} - {Message}. Diagnostics: {Diagnostics}", 
                    ex.StatusCode, ex.Message, ex.Diagnostics.ToString());
                throw;
            }
        }

        public async Task<IEnumerable<Employee>> GetEmployeesByDepartmentAsync(string department)
        {
            try
            {
                var queryDefinition = new QueryDefinition(
                    "SELECT * FROM c WHERE c.department = @department")
                    .WithParameter("@department", department);

                var query = _container.GetItemQueryIterator<Employee>(
                    queryDefinition, 
                    requestOptions: new QueryRequestOptions
                    {
                        PartitionKey = new PartitionKey(department),
                        MaxItemCount = 100
                    });

                var employees = new List<Employee>();
                while (query.HasMoreResults)
                {
                    var response = await query.ReadNextAsync();
                    employees.AddRange(response.ToList());
                    
                    // Log high latency operations
                    if (response.RequestCharge > 10)
                    {
                        _logger.LogWarning("High RU consumption: {RequestCharge} RUs. Diagnostics: {Diagnostics}", 
                            response.RequestCharge, response.Diagnostics.ToString());
                    }
                }

                return employees;
            }
            catch (CosmosException ex)
            {
                _logger.LogError("Error querying employees by department: {StatusCode} - {Message}. Diagnostics: {Diagnostics}", 
                    ex.StatusCode, ex.Message, ex.Diagnostics.ToString());
                throw;
            }
        }

        public async Task<IEnumerable<Employee>> GetAllEmployeesAsync()
        {
            try
            {
                var queryDefinition = new QueryDefinition("SELECT * FROM c");

                var query = _container.GetItemQueryIterator<Employee>(queryDefinition);
                var employees = new List<Employee>();
                
                while (query.HasMoreResults)
                {
                    var response = await query.ReadNextAsync();
                    employees.AddRange(response.ToList());
                    
                    // Log high latency operations
                    if (response.RequestCharge > 15)
                    {
                        _logger.LogWarning("High RU consumption for cross-partition query: {RequestCharge} RUs. Diagnostics: {Diagnostics}", 
                            response.RequestCharge, response.Diagnostics.ToString());
                    }
                }

                return employees;
            }
            catch (CosmosException ex)
            {
                _logger.LogError("Error querying all employees: {StatusCode} - {Message}. Diagnostics: {Diagnostics}", 
                    ex.StatusCode, ex.Message, ex.Diagnostics.ToString());
                throw;
            }
        }

        public async Task<Employee?> GetEmployeeByEmployeeIdAsync(string employeeId)
        {
            try
            {
                var queryDefinition = new QueryDefinition(
                    "SELECT * FROM c WHERE c.employeeId = @employeeId")
                    .WithParameter("@employeeId", employeeId);

                var query = _container.GetItemQueryIterator<Employee>(queryDefinition);
                
                while (query.HasMoreResults)
                {
                    var response = await query.ReadNextAsync();
                    var employee = response.FirstOrDefault();
                    
                    if (employee != null)
                    {
                        return employee;
                    }
                }

                return null;
            }
            catch (CosmosException ex)
            {
                _logger.LogError("Error querying employee by employeeId: {StatusCode} - {Message}. Diagnostics: {Diagnostics}", 
                    ex.StatusCode, ex.Message, ex.Diagnostics.ToString());
                throw;
            }
        }

        public async Task<Employee> UpdateEmployeeAsync(Employee employee)
        {
            try
            {
                var response = await _container.UpsertItemAsync(employee, new PartitionKey(employee.Department));
                return response.Resource;
            }
            catch (CosmosException ex)
            {
                _logger.LogError("Error updating employee: {StatusCode} - {Message}. Diagnostics: {Diagnostics}", 
                    ex.StatusCode, ex.Message, ex.Diagnostics.ToString());
                throw;
            }
        }

        public async Task DeleteEmployeeAsync(string id, string department)
        {
            try
            {
                await _container.DeleteItemAsync<Employee>(id, new PartitionKey(department));
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Employee already deleted or doesn't exist
                return;
            }
            catch (CosmosException ex)
            {
                _logger.LogError("Error deleting employee: {StatusCode} - {Message}. Diagnostics: {Diagnostics}", 
                    ex.StatusCode, ex.Message, ex.Diagnostics.ToString());
                throw;
            }
        }
    }
}