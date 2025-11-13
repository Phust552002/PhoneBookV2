using PhoneBook.Models;

public interface IPhoneBookRepository
{
    // BuildTree
    Task<List<Department>> GetDepartmentsAsync();
    // active employees 
    Task<List<Employee>> GetEmployeesByDepartmentAsync(int departmentId);
    Task<List<Employee>> GetAllEmployeesAsync();
    // Inactive employees
    Task<List<Employee>> GetAllInactiveEmployeesAsync();
    Task<List<Employee>> GetInactiveEmployeesByDepartmentAsync(int departmentId);
    // Authentication
    Task<Employee> GetEmployeeByUsernameAsync(string username);
    //Update
    Task<bool> UpdateEmployeeAsync(Employee employee);
    //Add roles
    Task<List<int>> GetUserRolesAsync(int userId);

}