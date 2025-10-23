using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace fierhub_authcheck_net.Middleware
{
    public class RouteValidator(IConfiguration configuration)
    {
        // private readonly List<string> _routes = new List<string>();

        public bool TestRoute(RequestDelegate next, HttpContext context)
        {
            var _authorizedPaths = configuration.GetSection("Authorize").Get<List<string>>() ?? new List<string>();
            return _authorizedPaths.Any(x => context.Request.Path.ToString().Contains(x, StringComparison.OrdinalIgnoreCase));
        }

        //public void TestConnection()
        //{
        //    if (string.IsNullOrEmpty(_currentSession.LocalConnectionString))
        //    {
        //        throw new EmstumException("Unable to find database detail. Please contact to admin.");
        //    }
        //}

        public bool TestAnonymous(RequestDelegate next, HttpContext context)
        {
            var endpoint = context.GetEndpoint();
            return endpoint?.Metadata?.GetMetadata<AllowAnonymousAttribute>() != null;
        }
    }
}
