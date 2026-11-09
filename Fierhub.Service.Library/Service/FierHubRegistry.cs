using Fierhub.Service.Library.IService;
using Fierhub.Service.Library.Middleware;
using Fierhub.Service.Library.Middleware.Service;
using Fierhub.Service.Library.Model;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Text;

namespace Fierhub.Service.Library.Service
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
            _builder.Services.AddScoped<UserSession>();
            _builder.Services.AddSingleton<RouteValidator>();
            _builder.Services.AddScoped<FierhubGatewayFilter>();
            _builder.Services.AddScoped<FierhubServiceFilter>();
            _builder.Services.AddScoped<FierhubCommonService>();

            _builder.Services.AddSingleton<FierhubServiceRequest>();
            _builder.Services.AddSingleton(x =>
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
            // RegisterJsonHandler();
        }

        private void ConfigureFierhub()
        {
            var serviceProvider = _builder.Services.BuildServiceProvider();
            var httpServiceRequest = serviceProvider.GetRequiredService<FierhubServiceRequest>();

            var fierHubConfigInstance = FierHubConfig.Instance();

            FierHubConfig fierHubConfig = _builder.Configuration.GetSection("FierHub").Get<FierHubConfig>() ?? new FierHubConfig();

            var fierHubSection = _builder.Configuration.GetSection("FierHub");
            if (fierHubSection.Exists())
            {
                foreach (var child in fierHubSection.GetChildren())
                {
                    if (!fierHubConfig.AdditionalConfiguration.ContainsKey(child.Key) &&
                        !IsStandardFierHubProperty(child.Key))
                    {
                        var token = ConfigurationSectionToJToken(child);
                        if (token != null)
                        {
                            fierHubConfig.AdditionalConfiguration[child.Key] = token;
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(fierHubConfig.Token))
                fierHubConfig = ReadConfigurationFile(fierHubConfig.Token, httpServiceRequest);

            if (fierHubConfig.ConfiguredFromFierhub)
            {
                fierHubConfig = GetConfigurationDetailFromFierhub(
                        httpServiceRequest,
                        fierHubConfig.Configuration?.Token ?? fierHubConfigInstance.Configuration?.Token,
                        fierHubConfig.Configuration?.FileName ?? fierHubConfigInstance.Configuration?.FileName
                    );
            }

            if (fierHubConfig.JwtSecret != null)
            {
                fierHubConfig.JwtSecret.IsPrimary = true;
                fierHubConfig.Secrets = new List<TokenRequestBody> { fierHubConfig.JwtSecret };
            }

            // ConfigurationFierhub(fierHubConfig, httpServiceRequest);
            // LoadJwtSecret(fierHubConfig, httpServiceRequest);

            if (fierHubConfig.Secrets != null && fierHubConfig.Secrets.Any(x => x.IsPrimary))
            {
                RegisterJWTTokenService(fierHubConfig.Secrets.FirstOrDefault(x => x.IsPrimary).Key);
            }

            fierHubConfigInstance.Initialize(
                fierHubConfig.Datasource,
                fierHubConfig.Authorize,
                fierHubConfig.Configuration,
                fierHubConfig.Connections,
                fierHubConfig.Secrets,
                fierHubConfig.ConnectionStrings,
                fierHubConfig.AdditionalConfiguration
            );

            fierHubConfigInstance.Token = fierHubConfig.Token;
            fierHubConfigInstance.JwtSecret = fierHubConfig.JwtSecret;
            fierHubConfigInstance.ConfigurationGateway = fierHubConfig.ConfigurationGateway;
            fierHubConfigInstance.ConfigurationService = fierHubConfig.ConfigurationService;
            fierHubConfigInstance.AppSettingFiles = fierHubConfig.AppSettingFiles;
            fierHubConfigInstance.Records = fierHubConfig.Records;
            fierHubConfigInstance.UseDbConfigFromFierhub = fierHubConfig.UseDbConfigFromFierhub;
            fierHubConfigInstance.UseTokenSecretFromFierhub = fierHubConfig.UseTokenSecretFromFierhub;
            fierHubConfigInstance.ConfiguredFromFierhub = fierHubConfig.ConfiguredFromFierhub;
            fierHubConfigInstance.IsFierhubDataLoadingCompleted = fierHubConfig.IsFierhubDataLoadingCompleted;

            fierHubConfig = null;
        }

        private void ConfigurationFierhub(FierHubConfig fierHubConfig, FierhubServiceRequest httpServiceRequest)
        {
            if (fierHubConfig.Datasource == null && fierHubConfig.Connections == null)
            {
                throw new Exception("Connection not found your fierhub configuration.");
            }

            fierHubConfig.ConfigureUses(httpServiceRequest);
        }

        private void LoadJwtSecret(FierHubConfig fierHubConfig, FierhubServiceRequest httpServiceRequest)
        {
            if (fierHubConfig.Configuration.FileName != null)
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

                if (fierHubConfig.Secrets == null) fierHubConfig.Secrets = new List<TokenRequestBody>();
                fierHubConfig.Secrets.Add(tokenRequestBody);
            }
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

            var responseJson = (string)responseModel!.responseBody!;
            var fierHubConfig = JsonConvert.DeserializeObject<FierHubConfig>(responseJson)!;
            if (fierHubConfig == null)
            {
                throw new Exception("Fail to get the Fierhub configuration from server.");
            }

            try
            {
                var jsonObj = JObject.Parse(responseJson);
                if (jsonObj["FierHub"] is JObject innerFierHub)
                {
                    foreach (var prop in innerFierHub.Properties())
                    {
                        if (!fierHubConfig.AdditionalConfiguration.ContainsKey(prop.Name))
                        {
                            fierHubConfig.AdditionalConfiguration[prop.Name] = prop.Value;
                        }
                    }
                }
            }
            catch
            {
                // Fallback to directly deserialized object
            }

            return fierHubConfig;
        }

        private FierHubConfig ReadConfigurationFile(string token, FierhubServiceRequest httpServiceRequest)
        {
            var payload = new
            {
                accessToken = token,
            };

            var responseModel = httpServiceRequest.PostRequestAsync<ResponseModel>(
                    "https://www.fierhub.com/api/fileContent/readConfigurationFile",
                    JsonConvert.SerializeObject(payload)
                ).ConfigureAwait(false).GetAwaiter().GetResult();

            if (responseModel == null || responseModel.statusCode != 200)
            {
                throw new Exception("Fierhub Datasource must contain files, Please register with https://www.fierhub.com register and follow the step.");
            }

            var responseString = (string)responseModel!.responseBody!;
            var jsonObj = JObject.Parse(responseString);
            var fierHubToken = jsonObj["FierHub"] ?? jsonObj["fierhub"];

            FierHubConfig fierHubConfig;
            if (fierHubToken != null && fierHubToken.Type == JTokenType.Object)
            {
                fierHubConfig = fierHubToken.ToObject<FierHubConfig>() ?? new FierHubConfig();

                // Capture any additional sections at the root level outside "FierHub"
                foreach (var prop in jsonObj.Properties())
                {
                    if (!string.Equals(prop.Name, "FierHub", StringComparison.OrdinalIgnoreCase))
                    {
                        fierHubConfig.AdditionalConfiguration[prop.Name] = prop.Value;
                    }
                }
            }
            else
            {
                fierHubConfig = JsonConvert.DeserializeObject<FierHubConfig>(responseString) ?? new FierHubConfig();
            }

            return fierHubConfig;
        }

        private static bool IsStandardFierHubProperty(string key)
        {
            return _standardProperties.Contains(key);
        }

        private static readonly HashSet<string> _standardProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            nameof(FierHubConfig.Token),
            nameof(FierHubConfig.JwtSecret),
            nameof(FierHubConfig.Datasource),
            nameof(FierHubConfig.Authorize),
            nameof(FierHubConfig.ConfigurationGateway),
            nameof(FierHubConfig.ConfigurationService),
            nameof(FierHubConfig.Configuration),
            nameof(FierHubConfig.Connections),
            nameof(FierHubConfig.Secrets),
            nameof(FierHubConfig.AppSettingFiles),
            nameof(FierHubConfig.Records),
            nameof(FierHubConfig.ConnectionStrings),
            nameof(FierHubConfig.UseDbConfigFromFierhub),
            nameof(FierHubConfig.UseTokenSecretFromFierhub),
            nameof(FierHubConfig.IsFierhubDataLoadingCompleted),
            nameof(FierHubConfig.ConfiguredFromFierhub),
            nameof(FierHubConfig.AdditionalConfiguration)
        };

        private static JToken ConfigurationSectionToJToken(IConfigurationSection section)
        {
            if (section == null) return null;
            var children = section.GetChildren().ToList();
            if (!children.Any())
            {
                return section.Value != null ? JValue.CreateString(section.Value) : JValue.CreateNull();
            }

            bool isArray = children.All(c => int.TryParse(c.Key, out _));
            if (isArray)
            {
                var jArray = new JArray();
                foreach (var child in children.OrderBy(c => int.Parse(c.Key)))
                {
                    jArray.Add(ConfigurationSectionToJToken(child));
                }
                return jArray;
            }
            else
            {
                var jObj = new JObject();
                foreach (var child in children)
                {
                    jObj[child.Key] = ConfigurationSectionToJToken(child);
                }
                return jObj;
            }
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
                                    RoleClaimType = FierhubConstants.Roles
                                };
                            });
        }

        private void RegisterJsonHandler()
        {
            _builder.Services.AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                // This makes property names PascalCase globally
                options.SerializerSettings.ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = null // null → PascalCase (use C# property names as-is)
                };

                // Optional: allow camelCase JSON input to map to PascalCase model
                options.SerializerSettings.MetadataPropertyHandling = MetadataPropertyHandling.Ignore;
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