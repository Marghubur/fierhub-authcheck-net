using Fierhub.Service.Library.Middleware.Service;
using Fierhub.Service.Library.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Fierhub.Service.Library.Middleware
{
    public class FierhubRequestMiddleware(RequestDelegate _next)
    {
        public async Task Invoke(HttpContext context, FierhubServiceFilter serviceAuthorization)
        {
            try
            {
                context.Request.Headers.TryGetValue(FierhubConstants.Authorization, out StringValues authorization);
                serviceAuthorization.StoreClaims(authorization);

                await _next(context);
            }
            catch
            {
                throw;
            }
        }
    }

    public static class FierhubAuthentication
    {
        public static IApplicationBuilder UseFierhubAuthentication(this IApplicationBuilder app)
        {
            return app.UseMiddleware<FierhubRequestMiddleware>();
        }
    }
}