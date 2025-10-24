using fierhub_authcheck_net.Model;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace fierhub_authcheck_net.Middleware.Service
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
                handler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = fierHubConfig.JwtSecret.Issuer,
                    ValidAudience = fierHubConfig.JwtSecret.Issuer,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(fierHubConfig.JwtSecret.Key!))
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
