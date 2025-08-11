using Bt.Ems.Lib.PipelineConfig.DbConfiguration.Common;
using Bt.Ems.Lib.PipelineConfig.Model.Constants;
using Bt.Ems.Lib.PipelineConfig.Model.Constants.enums;
using Bt.Ems.Lib.PipelineConfig.Model.ExceptionModel;
using fierhub_authcheck_net.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using TimeZoneConverter;
using CurrentSession = fierhub_authcheck_net.Model.CurrentSession;

namespace fierhub_authcheck_net.Middleware
{
    public class RequestMiddleware(RequestDelegate _next,
                                   RouteValidator _routeValidator,
                                   IConfiguration _configuration)
    {
        private readonly bool isDbConfigure = _configuration.GetValue<bool>("FireHub:DatabaseConfiguration");
        public async Task Invoke(HttpContext context, CurrentSession currentSession, IDb db, TokenRequestBody tokenRequestBody)
        {
            try
            {
                var authorization = string.Empty;
                var requestType = string.Empty;
                var sessionJson = string.Empty;
                await _routeValidator.TestAnonymous(_next, context);

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
                                currentSession.LocalConnectionString = header.Value!;
                                break;
                            case nameof(RequestTypeEnum.MicroserviceRequest):
                                requestType = header.Value;
                                break;
                            case ApplicationConstants.SessionObject:
                                sessionJson = header.Value;
                                break;
                        }
                    }
                });

                await _routeValidator.TestConnection(currentSession);
                string userId = string.Empty;

                if (!string.IsNullOrEmpty(authorization))
                {
                    sessionJson = GetSessionJsonAsync(authorization, currentSession, tokenRequestBody);
                    LoadSession(sessionJson, currentSession);
                }
                else if (requestType == nameof(RequestTypeEnum.MicroserviceRequest))
                {
                    LoadSession(sessionJson, currentSession);
                }

                if (isDbConfigure)
                    db.SetupConnectionString(currentSession.LocalConnectionString);

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

        private string GetSessionJsonAsync(string authorization, CurrentSession currentSession, TokenRequestBody tokenRequestBody)
        {
            string sessionJson = string.Empty;
            string token = authorization.Replace(ApplicationConstants.JWTBearer, "").Trim();

            if (!string.IsNullOrEmpty(token) && token != "null")
            {

                var handler = new JwtSecurityTokenHandler();
                handler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = tokenRequestBody.Issuer,
                    ValidAudience = tokenRequestBody.Issuer,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenRequestBody.Key))
                }, out SecurityToken validatedToken);

                JwtSecurityToken securityToken = handler.ReadToken(token) as JwtSecurityToken;
                sessionJson = securityToken.Claims.FirstOrDefault(x => x.Type == ApplicationConstants.CurrentSession)!.Value;
            }

            return sessionJson;
        }

        private void LoadSession(string sessionJson, CurrentSession currentSession)
        {
            var session = JsonConvert.DeserializeObject<CurrentSession>(sessionJson);

            currentSession.TimeZoneName = session.TimeZoneName;
            currentSession.TimeZone = TZConvert.GetTimeZoneInfo(session.TimeZoneName);
            currentSession.TimeZoneNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, currentSession.TimeZone);
            currentSession.CompanyCode = session.CompanyCode;

            currentSession.FinancialStartYear = session.FinancialStartYear;
            currentSession.ManagerName = session.ManagerName;
            currentSession.FullName = session.FullName;
            currentSession.Mobile = session.Mobile;
            currentSession.Email = session.Email;
            currentSession.RoleId = session.RoleId;
            currentSession.ManagerEmail = session.ManagerEmail;
            currentSession.Culture = session.Culture;
            currentSession.ReportingManagerId = session.ReportingManagerId;
            currentSession.OrganizationId = session.OrganizationId;
            currentSession.DesignationId = session.DesignationId;
            currentSession.CompanyName = session.CompanyName;
            currentSession.CompanyId = session.CompanyId;
            currentSession.Authorization = session.Authorization;
            currentSession.EmployeeCodeLength = session.EmployeeCodeLength;
            currentSession.EmployeeCodePrefix = session.EmployeeCodePrefix;
            currentSession.UserId = session.UserId;
        }
    }
}
