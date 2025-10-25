using Bt.Ems.Lib.PipelineConfig.DbConfiguration.Model;
using fierhub_authcheck_net.Model;

namespace fierhub_authcheck_net.IService
{
    public interface IFierHubService
    {
        Task<FierhubAuthResponse> GenerateToken(object claims);
        Task<FierhubAuthResponse> GenerateToken(object claims, string userId);
        Task<FierhubAuthResponse> GenerateToken(object claims, List<string> roles);
        Task<FierhubAuthResponse> GenerateToken(object claims, string userId, List<string> roles);
    }
}
