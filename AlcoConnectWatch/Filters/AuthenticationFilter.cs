using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using AlcoConnectWatch.Services;

namespace AlcoConnectWatch.Filters
{
    /// <summary>
    /// Authentication filter that validates tokens on API requests
    /// </summary>
    public class AuthenticationFilter : IAuthenticationFilter
    {
        public bool AllowMultiple => false;

        public Task AuthenticateAsync(HttpAuthenticationContext context, CancellationToken cancellationToken)
        {
            var request = context.Request;

            // Check for Authorization header
            var authHeader = request.Headers.Authorization;

            if (authHeader == null || string.IsNullOrWhiteSpace(authHeader.Parameter))
            {
                // No token provided
                context.ErrorResult = new AuthenticationFailureResult("Missing authorization token", request);
                return Task.CompletedTask;
            }

            // Validate the token
            var token = authHeader.Scheme == "Bearer" ? authHeader.Parameter : authHeader.ToString();
            var result = AuthTokenService.ValidateToken(token);

            if (!result.IsValid)
            {
                context.ErrorResult = new AuthenticationFailureResult(result.Error, request);
                return Task.CompletedTask;
            }

            // Token is valid - set the principal
            var identity = new System.Security.Principal.GenericIdentity(result.Email);
            var principal = new System.Security.Principal.GenericPrincipal(identity, new[] { "User" });
            context.Principal = principal;

            // Store user info in request properties for controllers to access
            request.Properties["UserId"] = result.UserId;
            request.Properties["UserEmail"] = result.Email;

            return Task.CompletedTask;
        }

        public Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellationToken)
        {
            // Add WWW-Authenticate header for 401 responses
            context.Result = new AddChallengeOnUnauthorizedResult(context.Result);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Returns a 401 Unauthorized response with error message
    /// </summary>
    public class AuthenticationFailureResult : IHttpActionResult
    {
        private readonly string _reason;
        private readonly HttpRequestMessage _request;

        public AuthenticationFailureResult(string reason, HttpRequestMessage request)
        {
            _reason = reason;
            _request = request;
        }

        public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                RequestMessage = _request,
                Content = new StringContent($"{{\"error\": \"{_reason}\", \"success\": false}}")
            };
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            return Task.FromResult(response);
        }
    }

    /// <summary>
    /// Adds WWW-Authenticate header to unauthorized responses
    /// </summary>
    public class AddChallengeOnUnauthorizedResult : IHttpActionResult
    {
        private readonly IHttpActionResult _innerResult;

        public AddChallengeOnUnauthorizedResult(IHttpActionResult innerResult)
        {
            _innerResult = innerResult;
        }

        public async Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
        {
            var response = await _innerResult.ExecuteAsync(cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                response.Headers.WwwAuthenticate.Add(
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "realm=\"AlcoConnectWatch\""));
            }

            return response;
        }
    }

    /// <summary>
    /// Attribute to mark controllers/actions that require authentication
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class RequireAuthAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            var request = actionContext.Request;
            var authHeader = request.Headers.Authorization;

            if (authHeader == null || string.IsNullOrWhiteSpace(authHeader.Parameter))
            {
                actionContext.Response = new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("{\"error\": \"Authorization required\", \"success\": false}")
                };
                actionContext.Response.Content.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                return;
            }

            var token = authHeader.Scheme == "Bearer" ? authHeader.Parameter : authHeader.ToString();
            var result = AuthTokenService.ValidateToken(token);

            if (!result.IsValid)
            {
                actionContext.Response = new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent($"{{\"error\": \"{result.Error}\", \"success\": false}}")
                };
                actionContext.Response.Content.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                return;
            }

            // Store user info for controller access
            request.Properties["UserId"] = result.UserId;
            request.Properties["UserEmail"] = result.Email;

            base.OnActionExecuting(actionContext);
        }
    }
}
