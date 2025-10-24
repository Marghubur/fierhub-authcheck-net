using Bt.Ems.Lib.PipelineConfig.DbConfiguration.Service.HttpMicroserviceRequest;
using fierhub_authcheck_net.IService;
using fierhub_authcheck_net.Middleware;
using fierhub_authcheck_net.Middleware.Service;
using fierhub_authcheck_net.Model;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
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
        private WebApplicationBuilder _builder;

        public static FierHubRegistry Builder(WebApplicationBuilder builder)
        {
            return new FierHubRegistry(builder);
        }

        private FierHubRegistry(WebApplicationBuilder builder)
        {
            _builder = builder;


            _builder.Services.AddHttpClient();
            _builder.Services.AddHttpContextAccessor();
            _builder.Services.AddScoped<SessionDetail>();
            _builder.Services.AddSingleton<RouteValidator>();
            _builder.Services.AddScoped<FierhubGatewayFilter>();
            _builder.Services.AddScoped<FierhubServiceFilter>();
            _builder.Services.AddScoped<FierhubCommonService>();

            _builder.Services.AddSingleton<FierhubServiceRequest>();
            _builder.Services.AddSingleton<FierHubConfig>(x =>
            {
                return FierHubConfig.Instance();
            });

            _builder.Services.AddScoped<IFierHubService, FierHubService>(x =>
            {
                var httpServiceRequest = x.GetRequiredService<FierhubServiceRequest>();
                var fierHubConfig = x.GetRequiredService<FierHubConfig>();

                return new FierHubService(httpServiceRequest, fierHubConfig);
            });

            ConfigureFierhub();
            // RegisterJWTTokenService(fierHubConfig.Secrets.FirstOrDefault(x => x.IsPrimary).Key);
            RegisterJsonHandler();
        }

        private void ConfigureFierhub()
        {
            var serviceProvider = _builder.Services.BuildServiceProvider();
            var httpServiceRequest = serviceProvider.GetRequiredService<FierhubServiceRequest>();

            var fierHubConfigInstance = FierHubConfig.Instance();

            var fierHubConfig = _builder.Configuration.GetSection("FierHub").Get<FierHubConfig>();

            if (fierHubConfig.ConfigurationGateway == null && fierHubConfig.ConfigurationService == null)
            {
                throw new Exception("Pleasa add Configure section in you fierhub json object");
            }

            if (fierHubConfig.ConfigurationGateway != null && fierHubConfig.ConfigurationService != null)
            {
                throw new Exception("Can only configure either gateway or service");
            }

            if (fierHubConfig.ConfigurationGateway != null)
            {
                fierHubConfig.Configuration = fierHubConfig.ConfigurationGateway;
                fierHubConfig.Configuration.IsGatewayService = true;
            }
            else
            {
                fierHubConfig.Configuration = fierHubConfig.ConfigurationService;
                fierHubConfig.Configuration.IsGatewayService = false;
            }

            if (fierHubConfig.ConfiguredFromFierhub)
            {
                fierHubConfig = GetConfigurationDetailFromFierhub(
                        httpServiceRequest,
                        fierHubConfigInstance.Configuration.Token,
                        fierHubConfigInstance.Configuration.FileName
                    );
            }

            if (fierHubConfig.JwtSecret != null)
            {
                fierHubConfig.JwtSecret.IsPrimary = true;
                fierHubConfig.Secrets = new List<TokenRequestBody> { fierHubConfig.JwtSecret };
            }

            ConfigurationFierhub(fierHubConfig, httpServiceRequest);
            LoadJwtSecret(fierHubConfig, httpServiceRequest);
            RegisterJWTTokenService(fierHubConfig.Secrets.FirstOrDefault(x => x.IsPrimary).Key);

            fierHubConfigInstance.Initialize(
                fierHubConfig.Datasource,
                fierHubConfig.Authorize,
                fierHubConfig.Configuration,
                fierHubConfig.ConnectionDetails,
                fierHubConfig.Secrets
            );

            fierHubConfig = null;
        }

        private void ConfigurationFierhub(FierHubConfig fierHubConfig, FierhubServiceRequest httpServiceRequest)
        {
            var connections = _builder.Configuration
                .GetSection("ConnectionStrings")
                .Get<Dictionary<string, string>>();

            if (fierHubConfig.Datasource != null || connections != null)
            {
                fierHubConfig.ConfigureUses(httpServiceRequest, connections);
            }
        }

        private void LoadJwtSecret(FierHubConfig fierHubConfig, FierhubServiceRequest httpServiceRequest)
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

        private FierHubConfig GetConfigurationDetailFromFierhub(FierhubServiceRequest fierhubServiceRequest, string accessToken, string fileName)
        {
            var payload = new
            {
                accessToken,
                fileName
            };

            var responseModel = fierhubServiceRequest.PostRequestAsync<ResponseModel>(
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
            _builder.Services.AddAuthentication(x =>
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
            _builder.Services.AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                options.SerializerSettings.ContractResolver = new DefaultContractResolver();
            });

            _builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = false;
            });
        }

        #endregion
    }
}
