using System.Linq;
using System.Web.Http;
using AlcoConnectWatch.Data;
using AlcoConnectWatch.Models.DTOs;

namespace AlcoConnectWatch.Controllers
{
    [RoutePrefix("api/auth")]
    public class AuthController : ApiController
    {
        [HttpPost]
        [Route("login")]
        public IHttpActionResult Login(LoginRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                return BadRequest("Email and password are required");

            using (var db = new AlcoConnectWatchContext())
            {
                var user = db.Users.FirstOrDefault(u => u.Email == request.Email);
                if (user == null)
                    return Ok(new LoginResponse { Success = false, Message = "Invalid email or password" });

                var hash = DbInitializer.HashPassword(request.Password);
                if (user.PasswordHash != hash)
                    return Ok(new LoginResponse { Success = false, Message = "Invalid email or password" });

                var namePart = user.Email.Split('@')[0];
                var initials = string.Join("", namePart.Split('.').Select(p => p.Substring(0, 1).ToUpper()));
                if (initials.Length < 2) initials = namePart.Substring(0, 2).ToUpper();

                return Ok(new LoginResponse
                {
                    Success = true,
                    Message = "Login successful",
                    Token = System.Convert.ToBase64String(
                        System.Text.Encoding.UTF8.GetBytes($"{user.Id}:{user.Email}:{System.DateTime.UtcNow.Ticks}")),
                    User = new UserInfo
                    {
                        Name = namePart.Replace(".", " ").ToUpper(),
                        Email = user.Email,
                        Role = "Administrator",
                        Initials = initials
                    }
                });
            }
        }
    }
}
