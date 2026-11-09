using Fierhub.Service.Library.Service;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Reflection;

namespace Fierhub.Service.Library.Model
{
    public class FierHubConfig
    {
        public string Token { get; set; }
        public TokenRequestBody JwtSecret { get; set; }
        public List<DatasourceModel> Datasource { get; set; } = new List<DatasourceModel>();
        public AuthorizeModel Authorize { get; set; } = new AuthorizeModel();
        public ConfigurationModel ConfigurationGateway { get; set; }
        public ConfigurationModel ConfigurationService { get; set; }
        public ConfigurationModel Configuration { get; set; }
        public List<DatasourceModel> Connections { get; set; } = new List<DatasourceModel>();
        public List<TokenRequestBody> Secrets { get; set; } = new List<TokenRequestBody>();
        public List<string> AppSettingFiles { get; set; } = new List<string>();
        public Dictionary<string, KeyValueRecords> Records { set; get; } = new Dictionary<string, KeyValueRecords>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> ConnectionStrings { set; get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private IDictionary<string, JToken> _additionalConfiguration = new Dictionary<string, JToken>(StringComparer.OrdinalIgnoreCase);

        [JsonExtensionData]
        public IDictionary<string, JToken> AdditionalConfiguration
        {
            get => _additionalConfiguration;
            set
            {
                _additionalConfiguration = value != null
                    ? new Dictionary<string, JToken>(value, StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, JToken>(StringComparer.OrdinalIgnoreCase);
                _sectionCache?.Clear();
            }
        }

        [JsonIgnore]
        private readonly ConcurrentDictionary<string, object> _sectionCache = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        private static readonly object _lock = new object();
        private static FierHubConfig _instance = new FierHubConfig();

        public static FierHubConfig Instance()
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new FierHubConfig();
                    }
                }
            }

            return _instance;
        }

        internal static void ResetInstanceForTesting()
        {
            lock (_lock)
            {
                _instance = new FierHubConfig();
            }
        }

        public class KeyValueRecords
        {
            public string Key { get; set; }
            public string Value { get; set; }
        }

        public void Initialize(List<DatasourceModel> datasource,
            AuthorizeModel authorize,
            ConfigurationModel configuration,
            List<DatasourceModel> connectionDetails,
            List<TokenRequestBody> secrets,
            Dictionary<string, string> connectionStrings
        ) {
            Initialize(datasource, authorize, configuration, connectionDetails, secrets, connectionStrings, null);
        }

        public void Initialize(List<DatasourceModel> datasource,
            AuthorizeModel authorize,
            ConfigurationModel configuration,
            List<DatasourceModel> connectionDetails,
            List<TokenRequestBody> secrets,
            Dictionary<string, string> connectionStrings,
            IDictionary<string, JToken> additionalConfiguration
        ) {
            Datasource = datasource ?? new List<DatasourceModel>();
            Authorize = authorize ?? new AuthorizeModel();
            if (Authorize.Routes == null)
            {
                Authorize.Routes = new List<string>();
            }
            Configuration = configuration;
            Connections = connectionDetails ?? new List<DatasourceModel>();
            Secrets = secrets ?? new List<TokenRequestBody>();
            ConnectionStrings = connectionStrings ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (additionalConfiguration != null)
            {
                AdditionalConfiguration = new Dictionary<string, JToken>(additionalConfiguration, StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                AdditionalConfiguration = new Dictionary<string, JToken>(StringComparer.OrdinalIgnoreCase);
            }
            _sectionCache.Clear();
        }

        public class ConfigurationModel
        {
            public bool IsGatewayService { set; get; }
            public string Token { get; set; }
            public string FileName { get; set; }
        }

        public class DatasourceModel
        {
            public int Order { get; set; } = 0;
            public string Name { get; set; }
            public string Url { set; get; }
            public string ConnectionString { set; get; }
        }

        public class AuthorizeModel
        {
            public List<string> Routes { get; set; } = new List<string>();
        }

        public T GetSection<T>(string sectionPath, T defaultValue = default)
        {
            if (TryGetSection<T>(sectionPath, out var result))
            {
                return result;
            }
            return defaultValue;
        }

        public bool TryGetSection<T>(string sectionPath, out T result)
        {
            if (string.IsNullOrWhiteSpace(sectionPath))
            {
                result = default;
                return false;
            }

            string cacheKey = $"{typeof(T).FullName}::{sectionPath}";
            if (_sectionCache.TryGetValue(cacheKey, out var cachedValue))
            {
                if (cachedValue is T typedVal)
                {
                    result = typedVal;
                    return true;
                }
                if (cachedValue == null && default(T) == null)
                {
                    result = default;
                    return true;
                }
            }

            if (!TryFindToken(sectionPath, out JToken token) || token == null || token.Type == JTokenType.Null)
            {
                result = default;
                return false;
            }

            try
            {
                T deserialized;
                Type targetType = typeof(T);

                if (targetType == typeof(JToken))
                {
                    deserialized = (T)(object)token;
                }
                else if (targetType == typeof(JObject) && token is JObject jObj)
                {
                    deserialized = (T)(object)jObj;
                }
                else if (targetType == typeof(JArray) && token is JArray jArr)
                {
                    deserialized = (T)(object)jArr;
                }
                else if (targetType == typeof(string))
                {
                    if (token is JValue jVal)
                    {
                        deserialized = (T)(object)(jVal.Value?.ToString() ?? string.Empty);
                    }
                    else
                    {
                        deserialized = (T)(object)token.ToString();
                    }
                }
                else
                {
                    deserialized = token.ToObject<T>();
                }

                if (deserialized != null)
                {
                    _sectionCache[cacheKey] = deserialized;
                    result = deserialized;
                    return true;
                }
            }
            catch
            {
                // Safe non-throwing lookup on deserialization errors
            }

            result = default;
            return false;
        }

        private bool TryFindToken(string sectionPath, out JToken token)
        {
            token = null;
            if (string.IsNullOrWhiteSpace(sectionPath))
                return false;

            // 1. Direct match in AdditionalConfiguration
            if (AdditionalConfiguration != null)
            {
                foreach (var kvp in AdditionalConfiguration)
                {
                    if (string.Equals(kvp.Key, sectionPath, StringComparison.OrdinalIgnoreCase))
                    {
                        token = kvp.Value;
                        return true;
                    }
                }
            }

            var segments = sectionPath.Split(new[] { ':', '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
                return false;

            // 2. Check prefix matches in AdditionalConfiguration (e.g. "Parent:Child" or "Parent")
            if (AdditionalConfiguration != null)
            {
                for (int i = segments.Length; i >= 1; i--)
                {
                    string colonPrefix = string.Join(":", segments.Take(i));
                    string dotPrefix = string.Join(".", segments.Take(i));

                    foreach (var kvp in AdditionalConfiguration)
                    {
                        if (string.Equals(kvp.Key, colonPrefix, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(kvp.Key, dotPrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            var remaining = segments.Skip(i).ToArray();
                            if (remaining.Length == 0)
                            {
                                token = kvp.Value;
                                return true;
                            }
                            if (NavigateJToken(kvp.Value, remaining, out token))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            // 3. Fallback: Check strongly typed properties on FierHubConfig
            if (TryGetTokenFromProperties(segments, out token))
            {
                return true;
            }

            return false;
        }

        private static bool NavigateJToken(JToken current, string[] remainingSegments, out JToken result)
        {
            result = null;
            if (current == null) return false;

            JToken cursor = current;
            foreach (var seg in remainingSegments)
            {
                if (cursor == null) return false;

                if (cursor is JObject jObj)
                {
                    JProperty matchedProp = null;
                    foreach (var prop in jObj.Properties())
                    {
                        if (string.Equals(prop.Name, seg, StringComparison.OrdinalIgnoreCase))
                        {
                            matchedProp = prop;
                            break;
                        }
                    }

                    if (matchedProp == null) return false;
                    cursor = matchedProp.Value;
                }
                else if (cursor is JArray jArr)
                {
                    if (int.TryParse(seg, out int index) && index >= 0 && index < jArr.Count)
                    {
                        cursor = jArr[index];
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            result = cursor;
            return true;
        }

        private bool TryGetTokenFromProperties(string[] segments, out JToken token)
        {
            token = null;
            if (segments == null || segments.Length == 0) return false;

            var prop = typeof(FierHubConfig).GetProperty(segments[0],
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.IgnoreCase);

            if (prop != null)
            {
                var val = prop.GetValue(this);
                if (val == null) return false;

                try
                {
                    JToken rootToken = JToken.FromObject(val);
                    if (segments.Length == 1)
                    {
                        token = rootToken;
                        return true;
                    }

                    return NavigateJToken(rootToken, segments.Skip(1).ToArray(), out token);
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        private void CheckConfigurationSettings()
        {
            if (Configuration == null)
            {
                throw new Exception("No connection detail found, please add Connections or Fierhub Datasource.");
            }
        }

        public void ConfigureUses(FierhubServiceRequest httpServiceRequest)
        {
            // check configuraiton settings
            // CheckConfigurationSettings();

            // Connections ??= new List<DatasourceModel>();
            // LoadDatabaseProperties(httpServiceRequest);
            // ValidateDatasourceOrder();
        }

        private void ValidateDatasourceOrder()
        {
            if (Connections != null && Connections.Any())
            {
                var filteredConnection = Connections.Where(x => x.Order > 0).ToList();
                var uniqeOrder = new HashSet<int>();
                foreach (var item in filteredConnection)
                {
                    if (!uniqeOrder.Add(item.Order))
                        throw new Exception($"Duplicate order of {item.Name} datasource");
                }
            }
        }

        public void LoadDatabaseProperties(FierhubServiceRequest httpServiceRequest)
        {
            LoadDatabasePropertiesAsync(httpServiceRequest).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public async Task LoadDatabasePropertiesAsync(FierhubServiceRequest httpServiceRequest)
        {
            if (Datasource == null || !Datasource.Any())
            {
                return;
            }

            CheckConfigurationSettings();
            Connections ??= new List<DatasourceModel>();

            foreach (var datasource in Datasource)
            {
                var payload = new
                {
                    accessToken = Configuration.Token,
                    fileName = datasource.Url
                };

                var responseModel = await httpServiceRequest.PostRequestAsync<ResponseModel>(
                        "https://www.fierhub.com/api/fileContent/readFile",
                        JsonConvert.SerializeObject(payload)
                    ).ConfigureAwait(false);

                if (responseModel == null || responseModel.statusCode != 200)
                {
                    throw new Exception("Fierhub Datasource must contain files, Please register with https://www.fierhub.com register and follow the step.");
                }

                DatabaseProperties databaseProperties = JsonConvert.DeserializeObject<DatabaseProperties>(
                    (string)responseModel!.responseBody!);

                if (databaseProperties == null)
                {
                    throw new Exception("Fierhub Datasource must contain files, Please register with https://www.fierhub.com register and follow the step.");
                }

                Connections.Add(new DatasourceModel
                {
                    ConnectionString = databaseProperties.BuildConnectionString(),
                    Name = datasource.Name,
                    Order = datasource.Order,
                });
            }
        }

        public bool UseDbConfigFromFierhub { get; set; }
        public bool UseTokenSecretFromFierhub { get; set; }
        public bool IsFierhubDataLoadingCompleted { get; set; } = false;
        public bool ConfiguredFromFierhub { set; get; } = false;
    }
}
