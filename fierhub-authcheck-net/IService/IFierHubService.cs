using Bt.Ems.Lib.PipelineConfig.DbConfiguration.Model;

namespace fierhub_authcheck_net.IService
{
    public interface IFierHubService
    {
        Task<ApiAuthResponse> GenerateToken(object claims);
        Task<ApiAuthResponse> GenerateToken(object claims, string userId);
        Task<ApiAuthResponse> GenerateToken(object claims, List<string> roles);
        Task<ApiAuthResponse> GenerateToken(object claims, string userId, List<string> roles);
    }
}
