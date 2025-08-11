using Bt.Ems.Lib.PipelineConfig.DbConfiguration.Model.MicroserviceModel;

namespace fierhub_authcheck_net.IService
{
    public interface IFierHubService
    {
        Task<ApiResponse> GenerateToken(Dictionary<string, object> claims);
    }
}
