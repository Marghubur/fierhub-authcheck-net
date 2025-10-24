using Bt.Ems.Lib.PipelineConfig.DbConfiguration.Service.HttpMicroserviceRequest;
using Newtonsoft.Json;

namespace fierhub_authcheck_net.Model
{
    public class FierHubConfig
    {
        public TokenRequestBody JwtSecret { get; set; }
        public DatasourceModel Datasource { get; set; }
        public AuthorizeModel Authorize { get; set; }
        public ConfigurationModel Configuration { get; set; }
        public List<ConnectionDetail> ConnectionDetails { get; set; }
        public List<TokenRequestBody> Secrets { get; set; }
        public Dictionary<string, string> Claims { set; get; }

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

        public class ConnectionDetail
        {
            public string Name { get; set; }
            public string ConnectionString { get; set; }
            public bool Primay { get; set; }
        }

        public class DatasourceModel
        {
            public List<string> Files { get; set; }
            public bool Primary { set; get; }
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

        public void ConfigureUses(FierhubServiceRequest httpServiceRequest, IDictionary<string, string> connections)
        {
            // check configuraiton settings
            CheckConfigurationSettings();

            ConnectionDetails = new List<ConnectionDetail>();
            if (connections == null)
            {
                if (Datasource == null)
                {
                    throw new Exception("No connection detail found, please add Connections or Fierhub Datasource.");
                }

                if (Datasource.Files == null || Datasource.Files.Count == 0)
                {
                    throw new Exception("Fierhub Datasource must contain files, Please register with https://www.fierhub.com register and follow the step.");
                }

                LoadDatabaseProperties(httpServiceRequest);
            }

            if (Datasource == null)
            {
                if (connections == null)
                {
                    throw new Exception("No connection detail found, please add Connections or Fierhub Datasource.");
                }

                BindConnections(connections);
            }

            LoadDatabaseProperties(httpServiceRequest);
            BindConnections(connections);
        }

        private void BindConnections(IDictionary<string, string> connections)
        {
            foreach (var conn in connections)
            {
                ConnectionDetails.Add(new ConnectionDetail
                {
                    ConnectionString = conn.Value,
                    Name = conn.Key,
                    Primay = false,
                });
            }
        }

        private void LoadDatabaseProperties(FierhubServiceRequest httpServiceRequest)
        {
            foreach (string file in Datasource.Files)
            {
                var payload = new
                {
                    accessToken = Configuration.Token,
                    fileName = file
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

                ConnectionDetails.Add(new ConnectionDetail
                {
                    ConnectionString = databaseProperties.BuildConnectionString(),
                    Name = file,
                    Primay = Datasource.Primary,
                });
            }
        }

        public bool UseDbConfigFromFierhub { get; set; }
        public bool UseTokenSecretFromFierhub { get; set; }
        public bool ConfiguredFromFierhub { set; get; } = false;
    }
}
