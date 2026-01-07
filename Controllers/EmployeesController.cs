using Microsoft.AspNetCore.Mvc;
using CosmosApp.Services;
using CosmosApp.Models;
using CosmosApp.DTOs;

namespace CosmosApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeesController : ControllerBase
    {
        private readonly IEmployeeCosmosDbService _employeeService;
        private readonly ILogger<EmployeesController> _logger;

        public EmployeesController(IEmployeeCosmosDbService employeeService, ILogger<EmployeesController> logger)
        {
            _employeeService = employeeService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<EmployeeResponse>>> GetAllEmployees()
        {
            try
            {
                var employees = await _employeeService.GetAllEmployeesAsync();
                var response = employees.Select(emp => new EmployeeResponse
                {
                    Id = emp.Id,
                    EmployeeId = emp.EmployeeId,
                    Name = emp.Name,
                    Department = emp.Department,
                    Email = emp.Email,
                    CreatedDate = emp.CreatedDate
                });

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all employees");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("department/{department}")]
        public async Task<ActionResult<IEnumerable<EmployeeResponse>>> GetEmployeesByDepartment(string department)
        {
            try
            {
                var employees = await _employeeService.GetEmployeesByDepartmentAsync(department);
                var response = employees.Select(emp => new EmployeeResponse
                {
                    Id = emp.Id,
                    EmployeeId = emp.EmployeeId,
                    Name = emp.Name,
                    Department = emp.Department,
                    Email = emp.Email,
                    CreatedDate = emp.CreatedDate
                });

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employees for department {Department}", department);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{department}/{id}")]
        public async Task<ActionResult<EmployeeResponse>> GetEmployee(string department, string id)
        {
            try
            {
                var employee = await _employeeService.GetEmployeeAsync(id, department);
                if (employee == null)
                {
                    return NotFound();
                }

                var response = new EmployeeResponse
                {
                    Id = employee.Id,
                    EmployeeId = employee.EmployeeId,
                    Name = employee.Name,
                    Department = employee.Department,
                    Email = employee.Email,
                    CreatedDate = employee.CreatedDate
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employee {EmployeeId} from department {Department}", id, department);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("employee/{employeeId}")]
        public async Task<ActionResult<EmployeeResponse>> GetEmployeeByEmployeeId(string employeeId)
        {
            try
            {
                var employee = await _employeeService.GetEmployeeByEmployeeIdAsync(employeeId);
                if (employee == null)
                {
                    return NotFound();
                }

                var response = new EmployeeResponse
                {
                    Id = employee.Id,
                    EmployeeId = employee.EmployeeId,
                    Name = employee.Name,
                    Department = employee.Department,
                    Email = employee.Email,
                    CreatedDate = employee.CreatedDate
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employee by employeeId {EmployeeId}", employeeId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<ActionResult<EmployeeResponse>> CreateEmployee([FromBody] CreateEmployeeRequest request)
        {
            try
            {
                // Check if employee ID already exists
                var existingEmployee = await _employeeService.GetEmployeeByEmployeeIdAsync(request.EmployeeId);
                if (existingEmployee != null)
                {
                    return Conflict($"Employee with ID {request.EmployeeId} already exists");
                }

                var employee = new Employee
                {
                    Id = Guid.NewGuid().ToString(),
                    EmployeeId = request.EmployeeId,
                    Name = request.Name,
                    Department = request.Department,
                    Email = request.Email,
                    CreatedDate = DateTime.UtcNow
                };

                var createdEmployee = await _employeeService.CreateEmployeeAsync(employee);

                var response = new EmployeeResponse
                {
                    Id = createdEmployee.Id,
                    EmployeeId = createdEmployee.EmployeeId,
                    Name = createdEmployee.Name,
                    Department = createdEmployee.Department,
                    Email = createdEmployee.Email,
                    CreatedDate = createdEmployee.CreatedDate
                };

                return CreatedAtAction(nameof(GetEmployee), 
                    new { department = createdEmployee.Department, id = createdEmployee.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating employee {EmployeeId}", request.EmployeeId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{department}/{id}")]
        public async Task<ActionResult<EmployeeResponse>> UpdateEmployee(string department, string id, [FromBody] UpdateEmployeeRequest request)
        {
            try
            {
                var existingEmployee = await _employeeService.GetEmployeeAsync(id, department);
                if (existingEmployee == null)
                {
                    return NotFound();
                }

                existingEmployee.Name = request.Name;
                existingEmployee.Department = request.Department;
                existingEmployee.Email = request.Email;

                var updatedEmployee = await _employeeService.UpdateEmployeeAsync(existingEmployee);

                var response = new EmployeeResponse
                {
                    Id = updatedEmployee.Id,
                    EmployeeId = updatedEmployee.EmployeeId,
                    Name = updatedEmployee.Name,
                    Department = updatedEmployee.Department,
                    Email = updatedEmployee.Email,
                    CreatedDate = updatedEmployee.CreatedDate
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating employee {EmployeeId} in department {Department}", id, department);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{department}/{id}")]
        public async Task<ActionResult> DeleteEmployee(string department, string id)
        {
            try
            {
                await _employeeService.DeleteEmployeeAsync(id, department);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting employee {EmployeeId} from department {Department}", id, department);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}