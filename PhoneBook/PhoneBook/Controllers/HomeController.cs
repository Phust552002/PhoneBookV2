using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using PhoneBook.Models;
using PhoneBook.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PhoneBook.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IApiService _apiService;

        public HomeController(IApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var fullName = User.FindFirst("FullName")?.Value ?? User.Identity.Name;
                var positionName = User.FindFirst("PositionName")?.Value ?? "Nhân viên";
                var isAdmin = User.FindFirst("IsAdmin")?.Value == "True";

                ViewBag.UserName = fullName;
                ViewBag.UserPosition = positionName;
                ViewBag.isAdmin = isAdmin;

                // Lấy JWT token từ claim và set vào ApiService
                var token = User.FindFirst("JwtToken")?.Value;
                if (!string.IsNullOrEmpty(token))
                {
                    _apiService.SetToken(token);
                }

                // GET /api/Departments
                var departments = await _apiService.GetAsync<List<Department>>("/api/Departments");
                return View(departments);
            }
            catch (HttpRequestException)
            {
                // token hết hạn, redirect về login
                return RedirectToAction("Login", "Account");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDepartments()
        {
            try
            {
                var token = User.FindFirst("JwtToken")?.Value;
                if (!string.IsNullOrEmpty(token))
                {
                    _apiService.SetToken(token);
                }

                // Gọi API: GET /api/Departments
                var departments = await _apiService.GetAsync<List<Department>>("/api/Departments");

                var tree = departments.Select(d => new
                {
                    id = d.DepartmentId,
                    text = d.DepartmentName,
                    parentId = d.ParentId,
                    expanded = false,
                    items = BuildTree(d.Children)
                });

                return Json(tree);
            }
            catch (HttpRequestException)
            {
                return Unauthorized();
            }
        }

        private List<object> BuildTree(List<Department> children)
        {
            if (children == null || children.Count == 0)
                return new List<object>();

            return children.Select(c => new
            {
                id = c.DepartmentId,
                text = c.DepartmentName,
                parentId = c.ParentId,
                expanded = false,
                items = BuildTree(c.Children)
            }).ToList<object>();
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployeesByDepartment(int departmentId)
        {
            try
            {
                var token = User.FindFirst("JwtToken")?.Value;
                if (!string.IsNullOrEmpty(token))
                {
                    _apiService.SetToken(token);
                }

                // GET /api/Departments/{departmentId}/employees
                var result = await _apiService.GetAsync<List<Employee>>($"/api/Departments/{departmentId}/employees");
                return Json(result);
            }
            catch (HttpRequestException)
            {
                return Unauthorized();
            }
        }
        [HttpPost]
        [HttpGet]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> GetAllEmployees()
        {
            try
            {
                var token = User.FindFirst("JwtToken")?.Value;
                if (!string.IsNullOrEmpty(token))
                {
                    _apiService.SetToken(token);
                }

                // GET /api/Employees
                var employees = await _apiService.GetAsync<List<Employee>>("/api/Employees");
                return Json(employees);
            }
            catch (HttpRequestException)
            {
                return Unauthorized();
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllInactiveEmployees()
        {
            try
            {
                var token = User.FindFirst("JwtToken")?.Value;
                if (!string.IsNullOrEmpty(token))
                {
                    _apiService.SetToken(token);
                }

                // GET /api/Employees/inactive
                var employees = await _apiService.GetAsync<List<Employee>>("/api/Employees/inactive");
                return Json(employees);
            }
            catch (HttpRequestException)
            {
                return Unauthorized();
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetInactiveEmployeesByDepartment(int departmentId)
        {
            try
            {
                var token = User.FindFirst("JwtToken")?.Value;
                if (!string.IsNullOrEmpty(token))
                {
                    _apiService.SetToken(token);
                }

                // GET /api/Departments/{departmentId}/inactive-employees
                var employees = await _apiService.GetAsync<List<Employee>>($"/api/Departments/{departmentId}/inactive-employees");
                return Json(employees);
            }
            catch (HttpRequestException)
            {
                return Unauthorized();
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateEmployee([DataSourceRequest] DataSourceRequest request, Employee employee)
        {
            var isAdmin = User.FindFirst("IsAdmin")?.Value == "True";
            if (!isAdmin)
            {
                ModelState.AddModelError("", "You do not have permission to update employee information.");
                return Json(new[] { employee }.ToDataSourceResult(request, ModelState));
            }

            if (employee == null || employee.UserId <= 0)
            {
                return Json(new[] { employee }.ToDataSourceResult(request));
            }

            if (!ModelState.IsValid)
            {
                return Json(new[] { employee }.ToDataSourceResult(request, ModelState));
            }

            try
            {
                var token = User.FindFirst("JwtToken")?.Value;
                if (!string.IsNullOrEmpty(token))
                {
                    _apiService.SetToken(token);
                }

                //  PUT /api/Employees/{userId}
                var result = await _apiService.PutAsync<Employee>($"/api/Employees/{employee.UserId}", employee);

                if (result != null)
                {
                    return Json(new[] { result }.ToDataSourceResult(request, ModelState));
                }
                else
                {
                    ModelState.AddModelError("", "Failed to update employee");
                    return Json(new[] { employee }.ToDataSourceResult(request, ModelState));
                }
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError("", $"API connection error: {ex.Message}");
                return Json(new[] { employee }.ToDataSourceResult(request, ModelState));
            }
        }

        public IActionResult SetLanguage(string culture, string returnUrl = "/")
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddMinutes(5) }
            );
            return LocalRedirect(returnUrl);
        }
    }
}