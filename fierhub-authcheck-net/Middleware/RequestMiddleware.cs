using Bt.Ems.Lib.PipelineConfig.Model.Constants;
using Bt.Ems.Lib.PipelineConfig.Model.Constants.enums;
using Bt.Ems.Lib.PipelineConfig.Model.ExceptionModel;
using fierhub_authcheck_net.Middleware.Service;
using fierhub_authcheck_net.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Security.Claims;
using SessionDetail = fierhub_authcheck_net.Model.SessionDetail;

namespace fierhub_authcheck_net.Middleware
{
    public class RequestMiddleware(RequestDelegate _next,
                                   RouteValidator _routeValidator)
    {
        const string IsGatewayEnabled = "IsGatewayEnabled";

        public async Task Invoke(HttpContext context,
            SessionDetail session,
            FierhubGatewayFilter gatewayAuthorization,
            FierhubServiceFilter serviceAuthorization,
            TokenRequestBody tokenRequestBody)
        {
            try
            {
                var authorization = string.Empty;
                var requestType = string.Empty;
                var sessionJson = string.Empty;
                var isGatewayEnabled = false;
                var claimsValue = string.Empty;

                await _routeValidator.TestAnonymous(_next, context);

                // By pass for the login
                await _routeValidator.TestRoute(_next, context);

                Parallel.ForEach(context.Request.Headers, header =>
                {
                    if (header.Value.FirstOrDefault() != null)
                    {
                        switch (header.Key.ToLower())
                        {
                            case ApplicationConstants.Authorization:
                                authorization = header.Value.FirstOrDefault();
                                break;
                            case ApplicationConstants.database:
                                session.LocalConnectionString = header.Value!;
                                break;
                            case IsGatewayEnabled:
                                isGatewayEnabled = true;
                                break;
                            case "X-Claims":
                                claimsValue = header.Value;
                                break;
                        }
                    }
                });

                if (!isGatewayEnabled)
                {
                    var isService = true;

                    if (!isService)
                    {
                        var claims = gatewayAuthorization.AuthorizationToken(authorization);
                        context.Request.Headers.Append("X-Gateway-Enalbed", "1");
                        context.Request.Headers.Append("X-Claims", JsonConvert.SerializeObject(claims));
                    }
                    else
                    {
                        _routeValidator.TestConnection();
                        var claims = gatewayAuthorization.AuthorizationToken(authorization);
                        serviceAuthorization.AuthorizationToken(claims);
                    }
                }
                else
                {
                    _routeValidator.TestConnection();
                    var mappedClaims = JsonConvert.DeserializeObject<Dictionary<string, string>>(claimsValue);
                    serviceAuthorization.AuthorizationToken(mappedClaims);
                }

                await _next(context);
            }
            catch (EmstumException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
