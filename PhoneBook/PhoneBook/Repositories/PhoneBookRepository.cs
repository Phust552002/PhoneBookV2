using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using System;
using System.Diagnostics;
using PhoneBook.Models;
using Microsoft.Extensions.Configuration;

public class PhoneBookRepository : IPhoneBookRepository
{
    private readonly string _connectionString;

    public PhoneBookRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("HRMDb");
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    //SP: Sel_GetDepartments
    public async Task<List<Department>> GetDepartmentsAsync()
    {
        using var conn = CreateConnection();
        var all = (await conn.QueryAsync<Department>(
            "Sel_GetDepartments",
            commandType: CommandType.StoredProcedure
        )).ToList();

        // Dựng cây (Tree View)
        var lookup = all.ToDictionary(d => d.DepartmentId);
        foreach (var dept in all)
        {
            if (dept.ParentId != -1 && lookup.ContainsKey(dept.ParentId))
            {
                lookup[dept.ParentId].Children.Add(dept);
            }
        }

        var roots = all.Where(d => d.ParentId == -1).ToList();
        return roots;
    }

    //SP: Sel_GetEmployeesByDepartment
    public async Task<List<Employee>> GetEmployeesByDepartmentAsync(int departmentId)
    {
        using var conn = CreateConnection();
        var result = (await conn.QueryAsync<Employee>(
            "Sel_GetEmployeesByDepartment",
            new { DepartmentId = departmentId, StatusList = "1" },
            commandType: CommandType.StoredProcedure
        )).ToList();
        return result;
    }

    //SP: Sel_GetAllEmployees
    public async Task<List<Employee>> GetAllEmployeesAsync()
    {
        using var connection = CreateConnection();
        var employees = (await connection.QueryAsync<Employee>(
            "Sel_GetAllEmployees",
            new { StatusList = "1" },
            commandType: CommandType.StoredProcedure
        )).ToList();
        return employees;
    }

    //SP: Sel_GetAllEmployees
    public async Task<List<Employee>> GetAllInactiveEmployeesAsync()
    {
        using var connection = CreateConnection();
        var employees = (await connection.QueryAsync<Employee>(
            "Sel_GetAllEmployees",
            new { StatusList = "0,11,12,13" },
            commandType: CommandType.StoredProcedure
        )).ToList();
        return employees;
    }

    //P: Sel_GetEmployeesByDepartment
    public async Task<List<Employee>> GetInactiveEmployeesByDepartmentAsync(int departmentId)
    {
        using var conn = CreateConnection();
        var result = (await conn.QueryAsync<Employee>(
            "Sel_GetEmployeesByDepartment",
            new { DepartmentId = departmentId, StatusList = "0,11,12,13" },
            commandType: CommandType.StoredProcedure
        )).ToList();
        return result;
    }

    //SP: Sel_GetEmployeeByUsername
    public async Task<Employee> GetEmployeeByUsernameAsync(string username)
    {
        using var conn = CreateConnection();
        var employee = await conn.QueryFirstOrDefaultAsync<Employee>(
            "Sel_GetEmployeeByUsername",
            new { Username = username },
            commandType: CommandType.StoredProcedure
        );
        return employee;
    }

    //SP: Upd_UpdateEmployee
    public async Task<bool> UpdateEmployeeAsync(Employee employee)
    {
        using var connection = CreateConnection();

        var result = await connection.QueryFirstOrDefaultAsync<int>(
            "Upd_UpdateEmployee",
            new
            {
                employee.UserId,
                employee.FullName,
                employee.WorkingPhone,
                employee.HandPhone,
                employee.BusinessEmail
            },
            commandType: CommandType.StoredProcedure
        );

        return result > 0;
    }

    //SP: Sel_GetUserRoles
    public async Task<List<int>> GetUserRolesAsync(int userId)
    {
        using var connection = CreateConnection();

        var roles = (await connection.QueryAsync<int>(
            "Sel_GetUserRoles",
            new { UserId = userId },
            commandType: CommandType.StoredProcedure
        )).ToList();

        return roles;
    }
}