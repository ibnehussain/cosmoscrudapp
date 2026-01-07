// Global variables
const API_BASE = '';
let currentEditingEmployee = null;

// Initialize the application
document.addEventListener('DOMContentLoaded', function() {
    // Set up form event listeners
    document.getElementById('employee-form').addEventListener('submit', handleEmployeeSubmit);
    
    // Load all employees on startup
    loadAllEmployees();
});

// Toast Messages
function showToast(message, type = 'info') {
    const toast = document.createElement('div');
    toast.className = `toast ${type}`;
    toast.innerHTML = `
        <strong>${type.charAt(0).toUpperCase() + type.slice(1)}:</strong> ${message}
    `;
    
    document.getElementById('toast-container').appendChild(toast);
    
    // Auto remove after 5 seconds
    setTimeout(() => {
        toast.remove();
    }, 5000);
}

// Loading state management
function showLoading() {
    document.getElementById('loading').style.display = 'block';
}

function hideLoading() {
    document.getElementById('loading').style.display = 'none';
}

// === EMPLOYEE MANAGEMENT ===

// Employee Form Management
function showAddEmployeeForm() {
    document.getElementById('add-employee-form').style.display = 'block';
    clearEmployeeForm();
    // Reset form title and button text for new employee
    document.querySelector('#add-employee-form h3').textContent = 'Add New Employee';
    document.querySelector('#employee-form button[type="submit"]').textContent = 'Create Employee';
}

function hideAddEmployeeForm() {
    document.getElementById('add-employee-form').style.display = 'none';
    currentEditingEmployee = null;
    clearEmployeeForm();
}

function clearEmployeeForm() {
    document.getElementById('employee-form').reset();
}

async function handleEmployeeSubmit(e) {
    e.preventDefault();
    
    const employeeData = {
        employeeId: document.getElementById('employee-employeeId').value,
        name: document.getElementById('employee-name').value,
        department: document.getElementById('employee-department').value,
        email: document.getElementById('employee-email').value
    };

    showLoading();

    try {
        if (currentEditingEmployee) {
            await updateEmployee(currentEditingEmployee.department, currentEditingEmployee.employeeId, employeeData);
            showToast('Employee updated successfully!', 'success');
        } else {
            await createEmployee(employeeData);
            showToast('Employee created successfully!', 'success');
        }
        
        hideAddEmployeeForm();
        loadAllEmployees(); // Refresh the list
    } catch (error) {
        console.error('Error handling employee:', error);
        showToast('Error saving employee: ' + error.message, 'error');
    } finally {
        hideLoading();
    }
}

async function createEmployee(employeeData) {
    const response = await fetch(`${API_BASE}/api/employees`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(employeeData)
    });

    if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`Failed to create employee: ${errorText}`);
    }

    return response.json();
}

async function updateEmployee(department, employeeId, employeeData) {
    const response = await fetch(`${API_BASE}/api/employees/${department}/${employeeId}`, {
        method: 'PUT',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(employeeData)
    });

    if (!response.ok) {
        throw new Error('Failed to update employee');
    }

    return response.json();
}

async function loadEmployees() {
    const department = document.getElementById('filter-department').value.trim();
    
    if (!department) {
        loadAllEmployees();
        return;
    }

    showLoading();

    try {
        const response = await fetch(`${API_BASE}/api/employees/${encodeURIComponent(department)}`);
        
        if (!response.ok) {
            throw new Error('Failed to load employees');
        }

        const employees = await response.json();
        displayEmployees(employees);
    } catch (error) {
        console.error('Error loading employees:', error);
        showToast('Error loading employees: ' + error.message, 'error');
        document.getElementById('employees-list').innerHTML = '<p class="error">Failed to load employees.</p>';
    } finally {
        hideLoading();
    }
}

async function loadAllEmployees() {
    showLoading();

    try {
        const response = await fetch(`${API_BASE}/api/employees`);
        
        if (!response.ok) {
            throw new Error('Failed to load employees');
        }

        const employees = await response.json();
        displayEmployees(employees);
    } catch (error) {
        console.error('Error loading all employees:', error);
        showToast('Error loading employees: ' + error.message, 'error');
        document.getElementById('employees-list').innerHTML = '<p class="error">Failed to load employees.</p>';
    } finally {
        hideLoading();
    }
}

function displayEmployees(employees) {
    const container = document.getElementById('employees-list');
    
    if (!employees || employees.length === 0) {
        container.innerHTML = '<p>No employees found.</p>';
        return;
    }

    const employeeCards = employees.map(employee => `
        <div class="item-card">
            <div class="item-header">
                <h4>${escapeHtml(employee.name)}</h4>
                <span class="item-id">ID: ${escapeHtml(employee.employeeId)}</span>
            </div>
            <div class="item-content">
                <p><strong>Department:</strong> ${escapeHtml(employee.department)}</p>
                <p><strong>Email:</strong> ${escapeHtml(employee.email)}</p>
                <p><strong>Created:</strong> ${new Date(employee.createdDate).toLocaleDateString()}</p>
            </div>
            <div class="item-actions">
                <button class="btn btn-secondary" onclick="editEmployee('${escapeHtml(employee.department)}', '${escapeHtml(employee.employeeId)}')">Edit</button>
                <button class="btn btn-danger" onclick="deleteEmployee('${escapeHtml(employee.department)}', '${escapeHtml(employee.employeeId)}')">Delete</button>
            </div>
        </div>
    `).join('');

    container.innerHTML = employeeCards;
}

async function editEmployee(department, employeeId) {
    showLoading();

    try {
        const response = await fetch(`${API_BASE}/api/employees/${encodeURIComponent(department)}/${encodeURIComponent(employeeId)}`);
        
        if (!response.ok) {
            throw new Error('Failed to load employee');
        }

        const employee = await response.json();
        
        // Populate the form with employee data
        document.getElementById('employee-employeeId').value = employee.employeeId;
        document.getElementById('employee-name').value = employee.name;
        document.getElementById('employee-department').value = employee.department;
        document.getElementById('employee-email').value = employee.email;
        
        currentEditingEmployee = { 
            department: employee.department, 
            employeeId: employee.employeeId 
        };
        
        showAddEmployeeForm();
        
        // Update form title and button text
        document.querySelector('#add-employee-form h3').textContent = 'Edit Employee';
        document.querySelector('#employee-form button[type="submit"]').textContent = 'Update Employee';
        
    } catch (error) {
        console.error('Error loading employee:', error);
        showToast('Error loading employee: ' + error.message, 'error');
    } finally {
        hideLoading();
    }
}

async function deleteEmployee(department, employeeId) {
    if (!confirm('Are you sure you want to delete this employee?')) {
        return;
    }

    showLoading();

    try {
        const response = await fetch(`${API_BASE}/api/employees/${encodeURIComponent(department)}/${encodeURIComponent(employeeId)}`, {
            method: 'DELETE'
        });

        if (!response.ok) {
            throw new Error('Failed to delete employee');
        }

        showToast('Employee deleted successfully!', 'success');
        loadAllEmployees(); // Refresh the list
    } catch (error) {
        console.error('Error deleting employee:', error);
        showToast('Error deleting employee: ' + error.message, 'error');
    } finally {
        hideLoading();
    }
}

// Utility Functions
function escapeHtml(text) {
    if (typeof text !== 'string') {
        return text;
    }
    const map = {
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#039;'
    };
    return text.replace(/[&<>"']/g, function(m) { return map[m]; });
}