using Fierhub.Service.Library.Model;

namespace Fierhub.Service.Library.Middleware.Service
{
    public class FierhubServiceFilter(FierhubCommonService _fierhubCommonService, UserSession _session)
    {
        public void StoreClaims(string authorization)
        {
            if (!string.IsNullOrEmpty(authorization))
            {
                var claims = _fierhubCommonService.GetValidatedClaims(authorization);
                if (claims == null && claims.Count() == 0)
                {
                    throw new UnauthorizedAccessException("Claims not found in token.");
                }
                _session.Claims = claims;
            }
        }
    }
}
