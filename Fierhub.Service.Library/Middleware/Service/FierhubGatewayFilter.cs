namespace Fierhub.Service.Library.Middleware.Service
{
    public class FierhubGatewayFilter(FierhubCommonService _fierhubCommonService)
    {
        public bool ValidateToken(string authorization)
        {
            if (!string.IsNullOrEmpty(authorization))
            {
                return _fierhubCommonService.ValidateToken(authorization);
            }

            return false;
        }
    }
}
