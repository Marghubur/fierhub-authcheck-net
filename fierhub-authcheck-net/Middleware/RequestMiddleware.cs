using Bt.Ems.Lib.PipelineConfig.Model.Constants;
using Bt.Ems.Lib.PipelineConfig.Model.ExceptionModel;
using fierhub_authcheck_net.Middleware.Service;
using fierhub_authcheck_net.Model;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace fierhub_authcheck_net.Middleware
{
    public class RequestMiddleware(RequestDelegate _next,
                                   FierHubConfig _fierHubConfig,
                                   RouteValidator _routeValidator
                                   )
    {
        const string IsGatewayEnabled = "IsGatewayEnabled";

        public async Task Invoke(HttpContext context,
                                 SessionDetail session,
                                 FierhubGatewayFilter gatewayAuthorization,
                                 FierhubServiceFilter serviceAuthorization,
                                 FierhubCommonService fierhubCommonService
                                 )
        {
            try
            {
                var authorization = string.Empty;
                var isGatewayEnabled = false;
                var claimsValue = string.Empty;

                // By pass anonymous
                if (_routeValidator.TestAnonymous(_next, context) || _routeValidator.TestRoute(_next, context))
                {
                    await _next(context);
                    return;
                }

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
                    var claims = gatewayAuthorization.AuthorizationToken(authorization);
                    if (!_fierHubConfig.IsApiGatewayEnable)
                    {
                        context.Request.Headers.Append("X-Gateway-Enalbed", "1");
                        context.Request.Headers.Append("X-Claims", JsonConvert.SerializeObject(claims));
                    }
                    else
                    {
                        //_routeValidator.TestConnection();
                        serviceAuthorization.AuthorizationToken(claims);
                    }
                }
                else
                {
                    //_routeValidator.TestConnection();
                    var mappedClaims = JsonConvert.DeserializeObject<Dictionary<string, string>>(claimsValue);
                    serviceAuthorization.AuthorizationToken(mappedClaims);
                }

                fierhubCommonService.LoadDbConfiguration();
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