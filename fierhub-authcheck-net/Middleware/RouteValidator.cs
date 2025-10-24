using fierhub_authcheck_net.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace fierhub_authcheck_net.Middleware
{
    public class RouteValidator(FierHubConfig _fierHubConfig)
    {
        // private readonly List<string> _routes = new List<string>();

        public bool TestRoute(RequestDelegate next, HttpContext context)
        {
            return _fierHubConfig.Authorize.Routes.Any(x => context.Request.Path.ToString().Contains(x, StringComparison.OrdinalIgnoreCase));
        }

        public bool TestAnonymous(RequestDelegate next, HttpContext context)
        {
            var endpoint = context.GetEndpoint();
            return endpoint?.Metadata?.GetMetadata<AllowAnonymousAttribute>() != null;
        }
    }
}
