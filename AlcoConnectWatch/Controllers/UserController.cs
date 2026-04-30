using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using AlcoConnectWatch.Data;
using AlcoConnectWatch.Models;
using AlcoConnectWatch.Models.DTOs;

namespace AlcoConnectWatch.Controllers
{
    [RoutePrefix("api/users")]
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

            using (var db = new AlcoConnectWatchContext())
            {
                if (db.Users.Any(u => u.Email == request.Email))
                    return BadRequest("A user with this email already exists");

                var user = new User
                {
                    Email = request.Email,
                    PasswordHash = DbInitializer.HashPassword(request.Password),
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
            if (request == null) return BadRequest("Request body is required");

            using (var db = new AlcoConnectWatchContext())
            {
                var user = db.Users.Find(id);
                if (user == null) return NotFound();

                if (!string.IsNullOrEmpty(request.Email))
                    user.Email = request.Email;

                if (!string.IsNullOrEmpty(request.Password))
                    user.PasswordHash = DbInitializer.HashPassword(request.Password);

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
