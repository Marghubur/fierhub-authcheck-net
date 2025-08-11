using Bt.Ems.Lib.PipelineConfig.DbConfiguration.Model.MicroserviceModel;
using Bt.Ems.Lib.PipelineConfig.DbConfiguration.Service.HttpMicroserviceRequest;
using fierhub_authcheck_net.IService;
using fierhub_authcheck_net.Model;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace fierhub_authcheck_net.Service
{
    public class FierHubService(IConfiguration _configuration,
                                IHttpServiceRequest _httpServiceRequest,
                                TokenRequestBody _tokenRequestBody) : IFierHubService
    {
        private readonly string tokenManagerURL = _configuration.GetValue<string>("FireHub:TokenManager")!;
        public async Task<ApiResponse> GenerateToken(Dictionary<string, object> claims)
        {
            TokenRequestBody tokenRequestBody = new TokenRequestBody
            {
                Claims = claims,
                CompanyCode = _tokenRequestBody.CompanyCode,
                ExpiryTimeInSeconds = _tokenRequestBody.ExpiryTimeInSeconds,
                FileName = _tokenRequestBody.FileName,
                Issuer = _tokenRequestBody.Issuer,
                Key = _tokenRequestBody.Key,
                ParentId = _tokenRequestBody.ParentId,
                RefreshTokenExpiryTimeInSeconds = _tokenRequestBody.RefreshTokenExpiryTimeInSeconds,
                RepositoryId = _tokenRequestBody.RepositoryId,
                Roles = _tokenRequestBody.Roles,
                TokenName = _tokenRequestBody.TokenName
            };

            var result  = await _httpServiceRequest.PostRequestAsync<ApiResponse>(new ServicePayload
            {
                Endpoint = tokenManagerURL,
                Payload = JsonConvert.SerializeObject(tokenRequestBody)
            }, false);

            return result;
        }
    }
}
