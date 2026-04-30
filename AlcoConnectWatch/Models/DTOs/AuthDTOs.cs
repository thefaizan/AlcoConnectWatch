namespace AlcoConnectWatch.Models.DTOs
{
    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Token { get; set; }
        public UserInfo User { get; set; }
    }

    public class UserInfo
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string Initials { get; set; }
    }
}
