using Bt.Ems.Lib.PipelineConfig.DbConfiguration.Common;
using Bt.Ems.Lib.PipelineConfig.Model.Constants;
using Bt.Ems.Lib.PipelineConfig.Model.ExceptionModel;
using fierhub_authcheck_net.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace fierhub_authcheck_net.Middleware.Service
{
    public class FierhubCommonService(IConfiguration _configuration,
        SessionDetail currentSession,
        TokenRequestBody tokenRequestBody,
        IDb db)
    {
        private readonly bool isDbConfigure = _configuration.GetValue<bool>("FireHub:DatabaseConfiguration");

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

        public void LoadConfiguration()
        {
            if (isDbConfigure)
            {
                db.SetupConnectionString(currentSession.LocalConnectionString);
            }
        }
    }
}
