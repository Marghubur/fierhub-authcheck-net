using Bt.Ems.Lib.PipelineConfig.DbConfiguration.Model;
using Bt.Ems.Lib.PipelineConfig.DbConfiguration.Model.MicroserviceModel;
using Bt.Ems.Lib.PipelineConfig.DbConfiguration.Service.HttpMicroserviceRequest;
using fierhub_authcheck_net.IService;
using fierhub_authcheck_net.Model;
using Newtonsoft.Json;
using System.Reflection;

namespace fierhub_authcheck_net.Service
{
    public class FierHubService(IHttpServiceRequest _httpServiceRequest,
                                TokenRequestBody _tokenRequestBody) : IFierHubService
    {
        private const string tokenManagerURL = "https://www.bottomhalf.in/bt/s3/ExternalTokenManager/generateToken";

        public async Task<ApiAuthResponse> GenerateToken(object claims)
        {
            return await Generate(claimData: claims);
        }

        public async Task<ApiAuthResponse> GenerateToken(object claims, string userId)
        {
            return await Generate(claims, userId);
        }

        public async Task<ApiAuthResponse> GenerateToken(object claims, List<string> roles)
        {
            return await Generate(claimData: claims, roles: roles);
        }

        public async Task<ApiAuthResponse> GenerateToken(object claims, string userId, List<string> roles)
        {
            return await Generate(claims, userId, roles);
        }

        public async Task<ApiAuthResponse> Generate(object claimData, string userId = null, List<string> roles = null)
        {
            var claims = ConvertObjectToDictionary(claimData);

            if (userId != null) claims.Add("fierhub_autogen_id", userId);
            if (roles != null) claims.Add("fierhub_autogen_roles", roles.Aggregate((x, y) => x + "," + y));

            TokenRequestBody tokenRequestBody = new TokenRequestBody
            {
                Claims = claims,
                ExpiryTimeInSeconds = _tokenRequestBody.ExpiryTimeInSeconds,
                Issuer = _tokenRequestBody.Issuer,
                Key = _tokenRequestBody.Key,
                RefreshTokenExpiryTimeInSeconds = _tokenRequestBody.RefreshTokenExpiryTimeInSeconds,
            };

            var result = await _httpServiceRequest.PostRequestAsync<ApiAuthResponse>(new ServicePayload
            {
                Endpoint = tokenManagerURL,
                Payload = JsonConvert.SerializeObject(tokenRequestBody)
            }, false);

            return result;
        }

        public Dictionary<string, string> ConvertObjectToDictionary(object obj)
        {
            var dict = new Dictionary<string, string>();
            if (obj == null)
                return dict;

            Type type = obj.GetType();

            while (type != null)
            {
                // Get all fields
                FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
                foreach (var field in fields)
                {
                    try
                    {
                        object value = field.GetValue(obj);
                        dict[field.Name] = value != null ? value.ToString() : null;
                    }
                    catch
                    {
                        // Handle exceptions if needed
                    }
                }

                // Get all properties
                PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
                foreach (var prop in properties)
                {
                    try
                    {
                        if (prop.GetIndexParameters().Length == 0) // ignore indexers
                        {
                            object value = prop.GetValue(obj);
                            dict[prop.Name] = value != null ? value.ToString() : null;
                        }
                    }
                    catch
                    {
                        // Handle exceptions if needed
                    }
                }

                type = type.BaseType; // move to parent class
            }

            return dict;
        }
    }
}
