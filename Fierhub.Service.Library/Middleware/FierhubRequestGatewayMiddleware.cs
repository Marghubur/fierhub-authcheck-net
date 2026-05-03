using Fierhub.Service.Library.Middleware.Service;
using Fierhub.Service.Library.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

namespace Fierhub.Service.Library.Middleware
{
    public class FierhubRequestGatewayMiddleware(RequestDelegate _next, RouteValidator _routeValidator)
    {
        public async Task Invoke(HttpContext context, FierhubGatewayFilter gatewayAuthorization)
        {
            try
            {
                // By pass anonymous
                if (_routeValidator.TestAnonymous(_next, context) || _routeValidator.TestRoute(_next, context))
                {
                    await _next(context);
                    return;
                }

                context.Request.Headers.TryGetValue(FierhubConstants.Authorization, out StringValues authorization);
                if (string.IsNullOrEmpty(authorization))
                {
                    throw new UnauthorizedAccessException("Invalid token or token not found.");
                }

                var isValid = gatewayAuthorization.ValidateToken(authorization);
                if (!isValid)
                {
                    throw new UnauthorizedAccessException("Invalid token or token not found.");
                }

                await _next(context);
            }
            catch
            {
                throw;
            }
        }
    }

    public static class FierhubGatewayAuthentication
    {
        public static IApplicationBuilder UseFierhubGatewayAuthentication(this IApplicationBuilder app)
        {
            return app.UseMiddleware<FierhubRequestGatewayMiddleware>();
        }
    }
}