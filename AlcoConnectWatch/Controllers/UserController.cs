using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using AlcoConnectWatch.Data;
using AlcoConnectWatch.Models;
using AlcoConnectWatch.Models.DTOs;
using AlcoConnectWatch.Filters;
using AlcoConnectWatch.Services;

namespace AlcoConnectWatch.Controllers
{
    [RoutePrefix("api/users")]
    [RequireAuth]
    public class UserController : ApiController
    {
        [HttpGet]
        [Route("")]
        public IHttpActionResult GetAll()
        {
            using (var db = new AlcoConnectWatchContext())
            {
                var users = db.Users
                    .OrderBy(u => u.Id)
                    .ToList()
                    .Select(u => new UserDTO
                    {
                        Id = u.Id,
                        Email = u.Email,
                        Password = new string('*', 8),
                        Sites = string.IsNullOrEmpty(u.SiteAccess)
                            ? new List<string>()
                            : u.SiteAccess.Split(',').Select(s => s.Trim()).ToList(),
                        CreatedAt = u.CreatedAt.ToString("yyyy-MM-dd")
                    })
                    .ToList();

                return Ok(users);
            }
        }

        [HttpPost]
        [Route("")]
        public IHttpActionResult Create(CreateUserRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                return BadRequest("Email and password are required");

            // Validate password strength
            if (request.Password.Length < 6)
                return BadRequest("Password must be at least 6 characters");

            using (var db = new AlcoConnectWatchContext())
            {
                if (db.Users.Any(u => u.Email == request.Email))
                    return BadRequest("A user with this email already exists");

                // Generate salt and hash password
                var salt = AuthTokenService.GenerateSalt();
                var hashedPassword = AuthTokenService.HashPassword(request.Password, salt);

                var user = new User
                {
                    Email = request.Email,
                    PasswordHash = hashedPassword,
                    PasswordSalt = salt,
                    SiteAccess = request.Sites != null ? string.Join(",", request.Sites) : "",
                    CreatedAt = DateTime.Now
                };

                db.Users.Add(user);
                db.SaveChanges();

                return Ok(new { success = true, id = user.Id });
            }
        }

        [HttpPut]
        [Route("{id:int}")]
        public IHttpActionResult Update(int id, UpdateUserRequest request)
        {
            return UpdateUserInternal(id, request);
        }

        // POST alternative for servers that block PUT requests
        [HttpPost]
        [Route("{id:int}/update")]
        public IHttpActionResult UpdateViaPost(int id, UpdateUserRequest request)
        {
            return UpdateUserInternal(id, request);
        }

        private IHttpActionResult UpdateUserInternal(int id, UpdateUserRequest request)
        {
            if (request == null) return BadRequest("Request body is required");

            using (var db = new AlcoConnectWatchContext())
            {
                var user = db.Users.Find(id);
                if (user == null) return NotFound();

                if (!string.IsNullOrEmpty(request.Email))
                    if (db.Users.Any(u => u.Email == request.Email && u.Id != id))
                        return BadRequest("A user with this email already exists");
                user.Email = request.Email;

                if (!string.IsNullOrEmpty(request.Password))
                {
                    // Generate new salt and hash for password update
                    var newSalt = AuthTokenService.GenerateSalt();
                    user.PasswordSalt = newSalt;
                    user.PasswordHash = AuthTokenService.HashPassword(request.Password, newSalt);
                }

                if (request.Sites != null)
                    user.SiteAccess = string.Join(",", request.Sites);

                db.SaveChanges();
                return Ok(new { success = true });
            }
        }

        [HttpDelete]
        [Route("{id:int}")]
        public IHttpActionResult Delete(int id)
        {
            return DeleteUserInternal(id);
        }

        // POST alternative for servers that block DELETE requests
        [HttpPost]
        [Route("{id:int}/delete")]
        public IHttpActionResult DeleteViaPost(int id)
        {
            return DeleteUserInternal(id);
        }

        private IHttpActionResult DeleteUserInternal(int id)
        {
            using (var db = new AlcoConnectWatchContext())
            {
                var user = db.Users.Find(id);
                if (user == null) return NotFound();

                db.Users.Remove(user);
                db.SaveChanges();
                return Ok(new { success = true });
            }
        }
    }
}
