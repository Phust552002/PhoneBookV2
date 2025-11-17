using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using PhoneBook.Models;
using PhoneBook.Models.DTOs;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
namespace PhoneBook.Services
{
    public interface IApiService
    {
        Task<T> GetAsync<T>(string endpoint);
        Task<T> PostAsync<T>(string endpoint, object data);
        Task<T> PutAsync<T>(string endpoint, object data);
        Task<bool> DeleteAsync(string endpoint);
        Task<string> GetAdminTokenAsync();
        Task<AccountLoginResponse> LoginWithTokenAsync(string username, string password, bool rememberMe = false);
        void SetToken(string token);
        string GetToken();
    }

    public class ApiService : IApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string TokenSessionKey = "JwtToken";
        private readonly CredentialsConfig _adminConfig;

        public ApiService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IOptions<CredentialsConfig> adminConfig)
        {
            _httpClient = httpClient;
            _adminConfig = adminConfig.Value;
            _httpContextAccessor = httpContextAccessor;
        }

        // Bước 1: Đăng nhập với admin để lấy JWT token
        public async Task<string> GetAdminTokenAsync()
        {
            var adminLoginData = new
            {
                username = _adminConfig.Username,
                password = _adminConfig.Password
            };
            var content = new StringContent(
                JsonSerializer.Serialize(adminLoginData),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync("/api/Auth/login", content);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var result = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(result, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (!string.IsNullOrEmpty(tokenResponse?.Token))
            {
                SetToken(tokenResponse.Token);
            }

            return tokenResponse?.Token;
        }

        // Bước 2: Dùng JWT token để gọi Account/login với username/password thật
        public async Task<AccountLoginResponse> LoginWithTokenAsync(string username, string password, bool rememberMe = false)
        {
            var loginData = new { username, password, rememberMe };
            var content = new StringContent(
                JsonSerializer.Serialize(loginData),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync("/api/Account/login", content);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var result = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AccountLoginResponse>(result, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        public void SetToken(string token)
        {
            _httpContextAccessor.HttpContext?.Session.SetString(TokenSessionKey, token);
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        public string GetToken()
        {
            var token = _httpContextAccessor.HttpContext?.Session.GetString(TokenSessionKey);
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }
            return token;
        }

        public async Task<T> GetAsync<T>(string endpoint)
        {
            GetToken(); // Đảm bảo token được set vào header
            var response = await _httpClient.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        public async Task<T> PostAsync<T>(string endpoint, object data)
        {
            GetToken();
            var content = new StringContent(
                JsonSerializer.Serialize(data),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync(endpoint, content);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(result, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        public async Task<T> PutAsync<T>(string endpoint, object data)
        {
            GetToken();
            var content = new StringContent(
                JsonSerializer.Serialize(data),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PutAsync(endpoint, content);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(result, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        public async Task<bool> DeleteAsync(string endpoint)
        {
            GetToken();
            var response = await _httpClient.DeleteAsync(endpoint);
            return response.IsSuccessStatusCode;
        }
    }

    public class TokenResponse
    {
        public string Token { get; set; }
        public DateTime Expiration { get; set; }
    }

    public class LoginResponse
    {
        public string Message { get; set; }
        public UserInfo User { get; set; }
    }

    public class UserInfo
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string EmployeeCode { get; set; }
        public string PositionName { get; set; }
        public int? DepartmentId { get; set; }
        public bool IsAdmin { get; set; }
        public List<int> Roles { get; set; }
    }
}