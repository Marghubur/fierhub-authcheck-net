using Fierhub.Service.Library.Model;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace Fierhub.Service.Library.Middleware.Service
{
    public class FierhubCommonService(FierHubConfig fierHubConfig)
    {

        public Dictionary<string, string> GetValidatedClaims(string authorization)
        {
            Dictionary<string, string> claims = new Dictionary<string, string>();
            string token = authorization.Replace("Bearer", "").Trim();

            if (!string.IsNullOrEmpty(token) && token != "null")
            {
                var handler = new JwtSecurityTokenHandler();
                if (fierHubConfig.Secrets == null || !fierHubConfig.Secrets.Any())
                    throw new Exception("Jwt secret not found");

                var jwtConfig = fierHubConfig.Secrets.Find(x => x.IsPrimary);
                if (jwtConfig == null)
                    throw new Exception("Primary jwt config not found");

                handler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtConfig.Issuer,
                    ValidAudience = jwtConfig.Issuer,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.Key!))
                }, out SecurityToken validatedToken);

                JwtSecurityToken securityToken = handler.ReadToken(token) as JwtSecurityToken;

                if (securityToken == null)
                {
                    throw new UnauthorizedAccessException("Authorization is invalid");
                }

                claims = securityToken!.Claims.ToDictionary(x => x.Type, x => x.Value);
            }

            return claims;
        }
    }
}
