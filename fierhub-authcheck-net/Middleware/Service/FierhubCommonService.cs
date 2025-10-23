using Bt.Ems.Lib.PipelineConfig.DbConfiguration.Common;
using Bt.Ems.Lib.PipelineConfig.DbConfiguration.Model.MicroserviceModel;
using Bt.Ems.Lib.PipelineConfig.DbConfiguration.Service.HttpMicroserviceRequest;
using Bt.Ems.Lib.PipelineConfig.Model;
using Bt.Ems.Lib.PipelineConfig.Model.Constants;
using Bt.Ems.Lib.PipelineConfig.Model.ExceptionModel;
using fierhub_authcheck_net.Model;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace fierhub_authcheck_net.Middleware.Service
{
    public class FierhubCommonService(FierHubConfig _fierHubConfig,
        SessionDetail currentSession,
        TokenRequestBody tokenRequestBody,
        IHttpServiceRequest _httpServiceRequest,
        IDb db)
    {

        public Dictionary<string, string> GetValidatedClaims(string authorization)
        {
            Dictionary<string, string> claims = new Dictionary<string, string>();
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
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenRequestBody.Key!))
                }, out SecurityToken validatedToken);

                JwtSecurityToken securityToken = handler.ReadToken(token) as JwtSecurityToken;

                if (securityToken == null)
                {
                    throw EmstumException.Unauthorized("Authorization is invalid");
                }

                claims = securityToken!.Claims.ToDictionary(x => x.Type, x => x.Value);
            }

            return claims;
        }

        public void LoadDbConfiguration()
        {
            if (_fierHubConfig.IsDatabaseConfigurationEnable)
            {
                if (!string.IsNullOrEmpty(_fierHubConfig.DbConfigFileName))
                {
                    GetConnectionString();
                }
                else if (!string.IsNullOrEmpty(currentSession.LocalConnectionString))
                {
                    db.SetupConnectionString(currentSession.LocalConnectionString);
                }
                else
                {

                }
            }
        }

        private void GetConnectionString()
        {
            var payload = new
            {
                accessToken = _fierHubConfig.Token,
                fileName = _fierHubConfig.DbConfigFileName
            };

            var responseModel = _httpServiceRequest.PostRequestAsync<ResponseModel>(new ServicePayload
            {
                Endpoint = "https://www.fierhub.com/api/fileContent/readFile",
                Payload = JsonConvert.SerializeObject(payload)
            }, false).ConfigureAwait(false).GetAwaiter().GetResult();

            var dbconfig = JsonConvert.DeserializeObject<DbConfig>((string)responseModel!.responseBody!)!;
            currentSession.LocalConnectionString = dbconfig.BuildConnectionString();
        }
    }
}
