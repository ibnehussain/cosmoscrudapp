using CosmosApp.Models;

namespace CosmosApp.Services
{
    public interface ICosmosDbService
    {
        Task<Item> CreateItemAsync(Item item);
        Task<Item?> GetItemAsync(string id, string userId);
        Task<IEnumerable<Item>> GetItemsByUserAsync(string userId);
        Task<Item> UpdateItemAsync(Item item);
        Task DeleteItemAsync(string id, string userId);
    }

    public interface IEmployeeCosmosDbService
    {
        Task<Employee> CreateEmployeeAsync(Employee employee);
        Task<Employee?> GetEmployeeAsync(string id, string department);
        Task<IEnumerable<Employee>> GetEmployeesByDepartmentAsync(string department);
        Task<IEnumerable<Employee>> GetAllEmployeesAsync();
        Task<Employee?> GetEmployeeByEmployeeIdAsync(string employeeId);
        Task<Employee> UpdateEmployeeAsync(Employee employee);
        Task DeleteEmployeeAsync(string id, string department);
    }
}