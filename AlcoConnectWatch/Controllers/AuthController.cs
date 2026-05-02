using System.Linq;
using System.Web.Http;
using AlcoConnectWatch.Data;
using AlcoConnectWatch.Models.DTOs;
using AlcoConnectWatch.Services;

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

                // Verify password with salt
                bool passwordValid;

                if (!string.IsNullOrEmpty(user.PasswordSalt))
                {
                    // New salted password verification
                    passwordValid = AuthTokenService.VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt);
                }
                else
                {
                    // Legacy password verification (for existing users without salt)
                    var hash = DbInitializer.HashPassword(request.Password);
                    passwordValid = user.PasswordHash == hash;

                    // Upgrade to salted password on successful login
                    if (passwordValid)
                    {
                        var newSalt = AuthTokenService.GenerateSalt();
                        user.PasswordSalt = newSalt;
                        user.PasswordHash = AuthTokenService.HashPassword(request.Password, newSalt);
                        db.SaveChanges();
                    }
                }

                if (!passwordValid)
                    return Ok(new LoginResponse { Success = false, Message = "Invalid email or password" });

                var namePart = user.Email.Split('@')[0];
                var initials = string.Join("", namePart.Split('.').Select(p => p.Substring(0, 1).ToUpper()));
                if (initials.Length < 2) initials = namePart.Substring(0, 2).ToUpper();

                // Generate secure signed token
                var token = AuthTokenService.GenerateToken(user.Id, user.Email);

                return Ok(new LoginResponse
                {
                    Success = true,
                    Message = "Login successful",
                    Token = token,
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

        [HttpPost]
        [Route("validate")]
        public IHttpActionResult ValidateToken()
        {
            var authHeader = Request.Headers.Authorization;
            if (authHeader == null || string.IsNullOrWhiteSpace(authHeader.Parameter))
                return Ok(new { valid = false, error = "No token provided" });

            var token = authHeader.Scheme == "Bearer" ? authHeader.Parameter : authHeader.ToString();
            var result = AuthTokenService.ValidateToken(token);

            return Ok(new
            {
                valid = result.IsValid,
                userId = result.UserId,
                email = result.Email,
                expiresAt = result.ExpiresAt,
                error = result.Error
            });
        }

        [HttpPost]
        [Route("logout")]
        public IHttpActionResult Logout()
        {
            // For stateless tokens, logout is handled client-side by removing the token
            // In a more advanced implementation, you could maintain a token blacklist
            return Ok(new { success = true, message = "Logged out successfully" });
        }
    }
}
