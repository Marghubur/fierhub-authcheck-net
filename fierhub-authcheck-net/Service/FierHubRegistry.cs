using Bt.Ems.Lib.PipelineConfig.DbConfiguration.Model;
using Bt.Ems.Lib.PipelineConfig.DbConfiguration.Model.MicroserviceModel;
using Bt.Ems.Lib.PipelineConfig.DbConfiguration.Service.HttpMicroserviceRequest;
using fierhub_authcheck_net.IService;
using fierhub_authcheck_net.Middleware;
using fierhub_authcheck_net.Middleware.Service;
using fierhub_authcheck_net.Model;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Text;
using CurrentSession = fierhub_authcheck_net.Model.SessionDetail;

namespace fierhub_authcheck_net.Service
{
    public class FierHubRegistry
    {
        private IWebHostEnvironment _env { set; get; }
        public IConfiguration _configuration { get; }
        private readonly IServiceCollection _services;

        public static FierHubRegistry Builder(IServiceCollection services, IWebHostEnvironment env, IConfiguration configuration)
        {
            return new FierHubRegistry(services, env, configuration);
        }

        private FierHubRegistry(IServiceCollection services, IWebHostEnvironment env, IConfiguration configuration)
        {
            _env = env;
            _configuration = configuration;
            _services = services;

            _services.AddSingleton<IHttpServiceRequest, HttpServiceRequest>();
            _services.AddScoped<FierhubGatewayFilter>();
            _services.AddScoped<FierhubServiceFilter>();
            _services.AddScoped<FierhubCommonService>();
            _services.AddScoped<IFierHubService, FierHubService>();
            _services.AddSingleton<RouteValidator>();

            RegisterPerSessionClass();
            RegisterTokenRequestClass();
            RegisterJsonHandler();
        }

        private void RegisterPerSessionClass()
        {
            _services.AddScoped(x =>
            {
                return new CurrentSession
                {
                    Environment = _env.EnvironmentName == nameof(DefinedEnvironments.Development) ?
                                    DefinedEnvironments.Development :
                                    DefinedEnvironments.Production
                };
            });
        }

        private void RegisterTokenRequestClass()
        {
            var tokenRequest = GetTokenRequest();
            _services.AddSingleton(x =>
            {
                return new TokenRequestBody
                {
                    CompanyCode = tokenRequest.CompanyCode,
                    ExpiryTimeInSeconds = tokenRequest.ExpiryTimeInSeconds,
                    FileName = tokenRequest.FileName,
                    Issuer = tokenRequest.Issuer,
                    Key = tokenRequest.Key,
                    ParentId = tokenRequest.ParentId,
                    RefreshTokenExpiryTimeInSeconds = tokenRequest.RefreshTokenExpiryTimeInSeconds,
                    RepositoryId = tokenRequest.RepositoryId,
                    Roles = tokenRequest.Roles,
                    TokenName = tokenRequest.TokenName
                };
            });

            RegisterJWTTokenService(tokenRequest.Key!);
        }

        private TokenRequestBody GetTokenRequest()
        {
            var serviceProvider = _services.BuildServiceProvider();
            var httpServiceRequest = serviceProvider.GetRequiredService<IHttpServiceRequest>();

            var tokenRepositoryUrl = _configuration.GetValue<string>("FireHub:TokenRepositoryUrl")!;
            string accessToken = _configuration.GetValue<string>("FireHub:Token")!;
            string requestBody = "{\"accessToken\": \"" + accessToken + "\"}";

            var responseModel = httpServiceRequest.PostRequestAsync<ResponseModel>(new ServicePayload
            {
                Endpoint = tokenRepositoryUrl,
                Payload = requestBody
            }, false).ConfigureAwait(false).GetAwaiter().GetResult();

            return JsonConvert.DeserializeObject<TokenRequestBody>((string)responseModel!.responseBody!)!;
        }

        #region JWTTOKEN_AND_JSON_CONFIGURATION

        private void RegisterJWTTokenService(string key)
        {
            _services.AddAuthentication(x =>
                {
                    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                            .AddJwtBearer(x =>
                            {
                                x.SaveToken = true;
                                x.RequireHttpsMetadata = false;
                                x.TokenValidationParameters = new TokenValidationParameters
                                {
                                    ValidateIssuer = false,
                                    ValidateAudience = false,
                                    ValidateLifetime = true,
                                    ValidateIssuerSigningKey = true,
                                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                                    ClockSkew = TimeSpan.Zero
                                };
                            });
        }

        private void RegisterJsonHandler()
        {
            _services.AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                options.SerializerSettings.ContractResolver = new DefaultContractResolver();
            });

            _services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = false;
            });
        }

        #endregion
    }
}
