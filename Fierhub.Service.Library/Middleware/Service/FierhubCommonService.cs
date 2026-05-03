using Fierhub.Service.Library.Model;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace Fierhub.Service.Library.Middleware.Service
{
    public class FierhubCommonService(FierHubConfig fierHubConfig)
    {

        public bool ValidateToken(string authorization)
        {
            if (string.IsNullOrWhiteSpace(authorization) || !authorization.StartsWith("Bearer "))
                return false;

            string token = authorization.Substring("Bearer ".Length).Trim();

            var handler = new JwtSecurityTokenHandler();

            var jwtConfig = fierHubConfig.Secrets?.FirstOrDefault(x => x.IsPrimary)
                ?? throw new Exception("Primary JWT config not found");

            try
            {
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtConfig.Key!)
                    ),

                    ValidateIssuer = true,
                    ValidIssuer = jwtConfig.Issuer,

                    ValidateAudience = true,
                    ValidAudience = jwtConfig.Audiance,

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                handler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

                // 🔒 Extra algorithm validation
                var jwtToken = validatedToken as JwtSecurityToken;
                if (jwtToken == null ||
                    jwtToken.Header.Alg != SecurityAlgorithms.HmacSha256)
                {
                    return false;
                }

                return true;
            }
            catch (SecurityTokenExpiredException)
            {
                throw new UnauthorizedAccessException("Token expired");
            }
            catch (SecurityTokenInvalidSignatureException)
            {
                throw new UnauthorizedAccessException("Token tampered");
            }
            catch (Exception ex)
            {
                throw new UnauthorizedAccessException("Invalid token: " + ex.Message);
            }
        }

        public Dictionary<string, string> GetValidatedClaims(string authorization)
        {
            if (string.IsNullOrWhiteSpace(authorization) || !authorization.StartsWith("Bearer "))
                throw new UnauthorizedAccessException("Invalid Authorization header");

            string token = authorization.Substring("Bearer ".Length).Trim();

            var handler = new JwtSecurityTokenHandler();

            var jwtConfig = fierHubConfig.Secrets?.FirstOrDefault(x => x.IsPrimary)
                ?? throw new Exception("Primary JWT config not found");

            try
            {
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtConfig.Key!)
                    ),

                    ValidateIssuer = true,
                    ValidIssuer = jwtConfig.Issuer,

                    ValidateAudience = true, // enable if needed
                    ValidAudience = jwtConfig.Audiance, // set if ValidateAudience is true

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                // 🔥 Validate token + get principal
                var principal = handler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

                // 🔒 Optional: Algorithm check
                var jwtToken = validatedToken as JwtSecurityToken;
                if (jwtToken == null ||
                    jwtToken.Header.Alg != SecurityAlgorithms.HmacSha256)
                {
                    throw new UnauthorizedAccessException("Invalid token algorithm");
                }

                // ✅ Convert claims to dictionary
                return principal.Claims
                    .GroupBy(c => c.Type)
                    .ToDictionary(
                        g => g.Key,
                        g => g.First().Value // take first if duplicate
                    );
            }
            catch (SecurityTokenExpiredException)
            {
                throw new UnauthorizedAccessException("Token expired");
            }
            catch (SecurityTokenInvalidSignatureException)
            {
                throw new UnauthorizedAccessException("Token tampered");
            }
            catch (Exception ex)
            {
                throw new UnauthorizedAccessException("Invalid token: " + ex.Message);
            }
        }
    }
}
