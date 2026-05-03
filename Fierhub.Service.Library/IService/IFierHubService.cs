using Fierhub.Service.Library.Model;

namespace Fierhub.Service.Library.IService
{
    public interface IFierHubService
    {
        Task<FierhubAuthResponse> GenerateToken(object claims, string audiance);
        Task<FierhubAuthResponse> GenerateToken(object claims, string audiance, string userId);
        Task<FierhubAuthResponse> GenerateToken(object claims, List<string> roles, string audiance);
        Task<FierhubAuthResponse> GenerateToken(object claims, string audiance, string userId, List<string> roles);
        Task<T> ReadConfiguration<T>(string fileCode);
    }
}
