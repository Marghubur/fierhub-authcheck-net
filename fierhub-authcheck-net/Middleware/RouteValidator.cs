using fierhub_authcheck_net.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace fierhub_authcheck_net.Middleware
{
    public class RouteValidator(FierHubConfig _fierHubConfig)
    {
        public bool TestRoute(RequestDelegate next, HttpContext context)
        {
            return _fierHubConfig?.Authorize?.Routes?.Any(x => context.Request.Path.ToString().Contains(x, StringComparison.OrdinalIgnoreCase)) ?? false;
        }

        public bool TestAnonymous(RequestDelegate next, HttpContext context)
        {
            var endpoint = context.GetEndpoint();
            return endpoint?.Metadata?.GetMetadata<AllowAnonymousAttribute>() != null;
        }
    }
}
