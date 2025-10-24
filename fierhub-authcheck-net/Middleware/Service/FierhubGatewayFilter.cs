namespace fierhub_authcheck_net.Middleware.Service
{
    public class FierhubGatewayFilter(FierhubCommonService _fierhubCommonService)
    {
        public Dictionary<string, string> ExtractClaims(string authorization)
        {
            Dictionary<string, string> claims = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(authorization))
            {
                claims = _fierhubCommonService.GetValidatedClaims(authorization);
            }

            return claims;
        }
    }
}
