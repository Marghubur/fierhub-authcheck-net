using Bt.Ems.Lib.PipelineConfig.DbConfiguration.Model.MicroserviceModel;

namespace fierhub_authcheck_net.IService
{
    public interface IFierHubService
    {
        Task<ApiResponse> GenerateToken(object claims);
        Task<ApiResponse> GenerateToken(object claims, string userId);
        Task<ApiResponse> GenerateToken(object claims, List<string> roles);
        Task<ApiResponse> GenerateToken(object claims, string userId, List<string> roles);
    }
}
