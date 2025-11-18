using Fierhub.Service.Library.Model;

namespace Fierhub.Service.Library.IService
{
    public interface IFierHubService
    {
        Task<FierhubAuthResponse> GenerateToken(object claims);
        Task<FierhubAuthResponse> GenerateToken(object claims, string userId);
        Task<FierhubAuthResponse> GenerateToken(object claims, List<string> roles);
        Task<FierhubAuthResponse> GenerateToken(object claims, string userId, List<string> roles);
        Task<T> ReadConfiguration<T>(string fileCode);
    }
}
