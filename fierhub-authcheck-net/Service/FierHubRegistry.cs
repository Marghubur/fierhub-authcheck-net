using Bt.Ems.Lib.PipelineConfig.DbConfiguration.Common;
using Bt.Ems.Lib.PipelineConfig.DbConfiguration.Model;
using Bt.Ems.Lib.PipelineConfig.DbConfiguration.Model.MicroserviceModel;
using Bt.Ems.Lib.PipelineConfig.DbConfiguration.Service.HttpMicroserviceRequest;
using Bt.Ems.Lib.PipelineConfig.DbConfiguration.Service.MySql.Code;
using fierhub_authcheck_net.IService;
using fierhub_authcheck_net.Middleware;
using fierhub_authcheck_net.Middleware.Service;
using fierhub_authcheck_net.Model;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Configuration;
using System.Text;

namespace fierhub_authcheck_net.Service
{
    public class FierHubRegistry
    {
        public IConfiguration _configuration { get; }
        private IServiceCollection _services;

        public static FierHubRegistry Builder(IServiceCollection services, IConfiguration configuration)
        {
            return new FierHubRegistry(services, configuration);
        }

        private FierHubRegistry(IServiceCollection services, IConfiguration configuration)
        {
            _configuration = configuration;
            _services = services;

            _services.AddSingleton<FierHubConfig>(x =>
            {
                var fierHubConfig = _configuration.GetSection("FierHub").Get<FierHubConfig>();                
                return fierHubConfig;
            });

            _services.AddHttpClient();
            _services.AddSingleton<IHttpServiceRequest, HttpServiceRequest>();
            _services.AddScoped<SessionDetail>();
            _services.AddSingleton<RouteValidator>();
            _services.AddScoped<FierhubGatewayFilter>();
            _services.AddScoped<FierhubServiceFilter>();
            _services.AddScoped<FierhubCommonService>();
            _services.AddScoped<IFierHubService, FierHubService>();
            _services.AddScoped<IDb, Db>();
            // RegisterPerSessionClass();

            ConfigurationFierhub();
            RegisterTokenRequestClass();
            RegisterJsonHandler();
        }

        //private void RegisterPerSessionClass()
        //{
        //    _services.AddScoped(x =>
        //    {
        //        return new CurrentSession
        //        {
        //            Environment = _env.EnvironmentName == nameof(DefinedEnvironments.Development) ?
        //                            DefinedEnvironments.Development :
        //                            DefinedEnvironments.Production
        //        };
        //    });
        //}

        private void ConfigurationFierhub()
        {
            var serviceProvider = _services.BuildServiceProvider();
            var httpServiceRequest = serviceProvider.GetRequiredService<IHttpServiceRequest>();
            var fierHubConfig = serviceProvider.GetRequiredService<FierHubConfig>();

            var connections = _configuration
                .GetSection("ConnectionStrings")
                .Get<Dictionary<string, string>>();

            fierHubConfig.ConfigureUses(httpServiceRequest, connections);
        }

        private void RegisterTokenRequestClass()
        {
            var tokenRequest = GetTokenRequest();
            _services.AddSingleton(x =>
            {
                return new TokenRequestBody
                {
                    Code = tokenRequest.Code,
                    ExpiryTimeInSeconds = tokenRequest.ExpiryTimeInSeconds,
                    Issuer = tokenRequest.Issuer,
                    Key = tokenRequest.Key,
                    Id = tokenRequest.Id,
                    RefreshTokenExpiryTimeInSeconds = tokenRequest.RefreshTokenExpiryTimeInSeconds,
                };
            });

            RegisterJWTTokenService(tokenRequest.Key!);
        }

        private TokenRequestBody GetTokenRequest()
        {
            var serviceProvider = _services.BuildServiceProvider();
            var fierHubConfig = serviceProvider.GetRequiredService<FierHubConfig>();

            var httpServiceRequest = serviceProvider.GetRequiredService<IHttpServiceRequest>();

            var config = _configuration.GetSection("FierHub").Get<FierHubConfig>();
            var payload = new
            {
                accessToken = config.Secrets.Token,
                fileName = config.Secrets.FileName
            };

            var responseModel = httpServiceRequest.PostRequestAsync<ResponseModel>(new ServicePayload
            {
                Endpoint = "https://www.fierhub.com/api/fileContent/readFile",
                Payload = JsonConvert.SerializeObject(payload)
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
                                    ClockSkew = TimeSpan.Zero,
                                    RoleClaimType = "fierhub_autogen_roles"
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
