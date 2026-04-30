using System.Collections.Generic;

namespace AlcoConnectWatch.Models.DTOs
{
    public class UserDTO
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public List<string> Sites { get; set; }
        public string CreatedAt { get; set; }
    }

    public class CreateUserRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public List<string> Sites { get; set; }
    }

    public class UpdateUserRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public List<string> Sites { get; set; }
    }
}
