using fierhub_authcheck_net.Middleware.Service;
using fierhub_authcheck_net.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

namespace fierhub_authcheck_net.Middleware
{
    public class RequestMiddleware(RequestDelegate _next, FierHubConfig _fierHubConfig, RouteValidator _routeValidator)
    {
        public async Task Invoke(HttpContext context,
                                 SessionDetail session,
                                 FierhubGatewayFilter gatewayAuthorization,
                                 FierhubServiceFilter serviceAuthorization
                                 )
        {
            try
            {
                // By pass anonymous
                if (_routeValidator.TestAnonymous(_next, context) || _routeValidator.TestRoute(_next, context))
                {
                    await _next(context);
                    return;
                }

                if (_fierHubConfig.Configuration.IsGatewayService)
                {
                    context.Request.Headers.TryGetValue(FierhubConstants.Authorization, out StringValues authorization);
                    if (string.IsNullOrEmpty(authorization))
                    {
                        throw new UnauthorizedAccessException("Invalid token or token not found.");
                    }

                    var claims = gatewayAuthorization.ExtractClaims(authorization);
                    context.Request.Headers.Append(FierhubConstants.GatewayEnabled, "1");
                    context.Request.Headers.Append(FierhubConstants.Claims, JsonConvert.SerializeObject(claims));
                }
                else
                {
                    context.Request.Headers.TryGetValue(FierhubConstants.Claims, out StringValues claimsValue);
                    if (string.IsNullOrEmpty(claimsValue))
                    {
                        throw new UnauthorizedAccessException("Claims not found in token.");
                    }

                    var mappedClaims = JsonConvert.DeserializeObject<Dictionary<string, string>>(claimsValue);
                    serviceAuthorization.StoreClaims(mappedClaims);
                }

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
            return app.UseMiddleware<RequestMiddleware>();
        }
    }
}