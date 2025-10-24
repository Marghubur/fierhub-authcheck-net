using Bt.Ems.Lib.PipelineConfig.DbConfiguration.Service.HttpMicroserviceRequest;
using fierhub_authcheck_net.IService;
using fierhub_authcheck_net.Middleware;
using fierhub_authcheck_net.Middleware.Service;
using fierhub_authcheck_net.Model;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
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
                var httpServiceRequest = x.GetRequiredService<FierhubServiceRequest>();

                if (fierHubConfig.ConfiguredFromFierhub)
                {
                    fierHubConfig = GetConfigurationDetailFromFierhub(fierHubConfig.Configuration.Token, fierHubConfig.Configuration.FileName);
                }

                if (fierHubConfig.JwtSecret != null)
                {
                    fierHubConfig.JwtSecret.IsPrimary = true;
                    fierHubConfig.Secrets = new List<TokenRequestBody> { fierHubConfig.JwtSecret };
                }

                ConfigurationFierhub(fierHubConfig, httpServiceRequest);
                RegisterTokenRequestClass(fierHubConfig, httpServiceRequest);
                return fierHubConfig;
            });

            _services.AddHttpClient();

            // Register IHttpContextAccessor
            _services.AddHttpContextAccessor();

            _services.AddSingleton<FierhubServiceRequest>();
            _services.AddScoped<SessionDetail>();
            _services.AddSingleton<RouteValidator>();
            _services.AddScoped<FierhubGatewayFilter>();
            _services.AddScoped<FierhubServiceFilter>();
            _services.AddScoped<FierhubCommonService>();
            _services.AddScoped<IFierHubService, FierHubService>();

            executeFierhubConfiguration();
            RegisterJsonHandler();
        }

        private void executeFierhubConfiguration()
        {
            var serviceProvider = _services.BuildServiceProvider();
            var config = serviceProvider.GetRequiredService<FierHubConfig>();
        }

        private void ConfigurationFierhub(FierHubConfig fierHubConfig, FierhubServiceRequest httpServiceRequest)
        {
            var connections = _configuration
                .GetSection("ConnectionStrings")
                .Get<Dictionary<string, string>>();

            if (fierHubConfig.Datasource != null || connections != null)
            {
                fierHubConfig.ConfigureUses(httpServiceRequest, connections);
            }
        }

        private void RegisterTokenRequestClass(FierHubConfig fierHubConfig, FierhubServiceRequest httpServiceRequest)
        {
            LoadTokenRequest(fierHubConfig, httpServiceRequest);
            var tokenRequest = fierHubConfig.Secrets.FirstOrDefault(x => x.IsPrimary);
            if (tokenRequest == null)
            {
                throw new Exception("No secret found in you configuration.");
            }

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

        private void LoadTokenRequest(FierHubConfig fierHubConfig, FierhubServiceRequest httpServiceRequest)
        {
            var payload = new
            {
                accessToken = fierHubConfig.Configuration.Token,
                fileName = fierHubConfig.Configuration.FileName
            };

            var responseModel = httpServiceRequest.PostRequestAsync<ResponseModel>(
                "https://www.fierhub.com/api/fileContent/readFile",
                JsonConvert.SerializeObject(payload)
            ).ConfigureAwait(false).GetAwaiter().GetResult();

            var tokenRequestBody = JsonConvert.DeserializeObject<TokenRequestBody>((string)responseModel!.responseBody!)!;
            if (tokenRequestBody == null)
            {
                throw new Exception("Token detail not found in fierhub server.");
            }

            fierHubConfig.Secrets.Add(tokenRequestBody);
        }

        private FierHubConfig GetConfigurationDetailFromFierhub(string accessToken, string fileName)
        {
            var serviceProvider = _services.BuildServiceProvider();
            var httpServiceRequest = serviceProvider.GetRequiredService<FierhubServiceRequest>();

            var payload = new
            {
                accessToken,
                fileName
            };

            var responseModel = httpServiceRequest.PostRequestAsync<ResponseModel>(
                "https://www.fierhub.com/api/fileContent/getConfiguration",
                JsonConvert.SerializeObject(payload)
            ).ConfigureAwait(false).GetAwaiter().GetResult();

            var fierHubConfig = JsonConvert.DeserializeObject<FierHubConfig>((string)responseModel!.responseBody!)!;
            if (fierHubConfig == null)
            {
                throw new Exception("Fail to get the Fierhub configuration from server.");
            }

            return fierHubConfig;
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
                                    ClockSkew = TimeSpan.FromSeconds(5),
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
