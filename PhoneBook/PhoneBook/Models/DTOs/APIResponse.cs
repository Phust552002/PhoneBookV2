namespace PhoneBook.Models.DTOs
{
    public class TokenResponse
    {
        public string Token { get; set; }
        public DateTime Expiration { get; set; }
    }

    public class AccountLoginResponse
    {
        public string Message { get; set; }
        public UserInfoDTO User { get; set; }
    }

    public class UserInfoDTO
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

    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public bool RememberMe { get; set; }
    }
}