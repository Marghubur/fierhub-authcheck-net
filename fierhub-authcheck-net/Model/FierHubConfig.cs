using fierhub_authcheck_net.Service;
using Newtonsoft.Json;

namespace fierhub_authcheck_net.Model
{
    public class FierHubConfig
    {
        public TokenRequestBody JwtSecret { get; set; }
        public List<DatasourceModel> Datasource { get; set; }
        public AuthorizeModel Authorize { get; set; }
        public ConfigurationModel ConfigurationGateway { get; set; }
        public ConfigurationModel ConfigurationService { get; set; }
        public ConfigurationModel Configuration { get; set; }
        public List<DatasourceModel> Connections { get; set; }
        public List<TokenRequestBody> Secrets { get; set; }
        public List<string> AppSettingFiles { get; set; }
        public Dictionary<string, string> Claims { set; get; }
        public Dictionary<string, KeyValueRecords> Records { set; get; }

        private static readonly object _lock = new object();
        private static FierHubConfig _instance = new FierHubConfig();

        public static FierHubConfig Instance()
        {
            if (_instance == null)
                lock (_lock)
                    if (_instance == null)
                        _instance = new FierHubConfig();

            return _instance;
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
            List<TokenRequestBody> secrets)
        {
            Datasource = datasource;
            Authorize = authorize;
            Configuration = configuration;
            Connections = connectionDetails;
            Secrets = secrets;
        }

        public T GetValue<T>(string key)
        {
            if (Claims == null)
            {
                throw new Exception("Claims are null or empty");
            }

            Claims!.TryGetValue(key, out var value);
            if (value == null)
            {
                return default;
            }

            return (T)Convert.ChangeType(value, typeof(T));
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
            public List<string> Routes { get; set; }
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
            CheckConfigurationSettings();

            Connections ??= new List<DatasourceModel>();
            LoadDatabaseProperties(httpServiceRequest);
        }

        private void LoadDatabaseProperties(FierhubServiceRequest httpServiceRequest)
        {
            foreach (var datasource in Datasource)
            {
                var payload = new
                {
                    accessToken = Configuration.Token,
                    fileName = datasource.Url
                };

                var responseModel = httpServiceRequest.PostRequestAsync<ResponseModel>(
                        "https://www.fierhub.com/api/fileContent/readFile",
                        JsonConvert.SerializeObject(payload)
                    ).ConfigureAwait(false).GetAwaiter().GetResult();

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
