using Bt.Ems.Lib.PipelineConfig.DbConfiguration.Model.MicroserviceModel;
using Bt.Ems.Lib.PipelineConfig.DbConfiguration.Service.HttpMicroserviceRequest;
using Bt.Ems.Lib.PipelineConfig.Model.ExceptionModel;
using Newtonsoft.Json;

namespace fierhub_authcheck_net.Model
{
    public class FierHubConfig
    {
        public Secret Secrets { get; set; }
        public DatasourceModel Datasource { get; set; }
        public AuthorizeModel Authorize { get; set; }
        public List<ConnectionDetail> ConnectionDetails { get; set; }

        public class ConnectionDetail
        {
            public string Name { get; set; }
            public string ConnectionString { get; set; }
            public bool Primay { get; set; }
        }

        public class Secret
        {
            public string Token { get; set; }
            public TokenRequestBody TokenSecret { get; set; }
            public string FileName { get; set; }
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

        public void ConfigureUses(IHttpServiceRequest httpServiceRequest, IDictionary<string, string> connections)
        {
            ConnectionDetails = new List<ConnectionDetail>();
            if (connections == null)
            {
                if (Datasource == null)
                {
                    throw EmstumException.BadRequest("No connection detail found, please add Connections or Fierhub Datasource.");
                }

                if (Datasource.Files == null || Datasource.Files.Count == 0)
                {
                    throw EmstumException.BadRequest("Fierhub Datasource must contain files, Please register with https://www.fierhub.com register and follow the step.");
                }

                LoadDatabaseProperties(httpServiceRequest);
            }

            if (Datasource == null)
            {
                if (connections == null)
                {
                    throw EmstumException.BadRequest("No connection detail found, please add Connections or Fierhub Datasource.");
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

        private void LoadDatabaseProperties(IHttpServiceRequest httpServiceRequest)
        {
            foreach (string file in Datasource.Files)
            {
                var payload = new
                {
                    accessToken = Secrets.Token,
                    fileName = file
                };

                var responseModel = httpServiceRequest.PostRequestAsync<ResponseModel>(new ServicePayload
                {
                    Endpoint = "https://www.fierhub.com/api/fileContent/readFile",
                    Payload = JsonConvert.SerializeObject(payload)
                }, false).ConfigureAwait(false).GetAwaiter().GetResult();

                if (responseModel == null || responseModel.statusCode != 200)
                {
                    throw EmstumException.BadRequest("Fierhub Datasource must contain files, Please register with https://www.fierhub.com register and follow the step.");
                }

                DatabaseProperties databaseProperties = JsonConvert.DeserializeObject<DatabaseProperties>(
                    (string)responseModel!.responseBody!);

                if (databaseProperties == null)
                {
                    throw EmstumException.BadRequest("Fierhub Datasource must contain files, Please register with https://www.fierhub.com register and follow the step.");
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

        public bool IsDatabaseConfigurationEnable { get; set; }
        public bool IsApiGatewayEnable { get; set; }
    }
}
