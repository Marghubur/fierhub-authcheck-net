using fierhub_authcheck_net.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Text;

namespace Bt.Ems.Lib.PipelineConfig.DbConfiguration.Service.HttpMicroserviceRequest
{
    public class FierhubServiceRequest
    {
        private readonly string ApplicationJson = @"application/json";
        public readonly string PlainText = @"text/plain";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<FierhubServiceRequest> _logger;
        private readonly JsonSerializerSettings _jsonSettings;

        public FierhubServiceRequest(IHttpClientFactory httpClientFactory, ILogger<FierhubServiceRequest> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<T> PutRequestAsync<T>(string endpoint, string payload)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var content = new StringContent(JsonConvert.SerializeObject(payload, _jsonSettings), Encoding.UTF8, ApplicationJson);

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, endpoint)
                {
                    Content = content
                };

                var response = await client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to call {endpoint}. Status: {response.StatusCode}");
                }

                return await GetResponseBody<T>(response);
            }
            catch
            {
                throw;
            }
        }

        public async Task<T> PostRequestAsync<T>(string endpoint, string payload)
        {
            try
            {
                _logger.LogInformation("Calling post request");

                var client = _httpClientFactory.CreateClient();

                var content = new StringContent(payload, Encoding.UTF8, FierhubConstants.ApplicationJson);

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = content
                };

                var response = await client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("HTTP POST to {Url} failed. Status: {StatusCode}, Response: {Response}", endpoint, response.StatusCode, errorContent);

                    throw new Exception($"Failed to call {endpoint}. Status: {response.StatusCode}");
                }

                return await GetResponseBody<T>(response);
            }
            catch
            {
                throw;
            }
        }

        public async Task<T> GetRequestAsync<T>(string endpoint)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, endpoint);

                var response = await client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to call {endpoint}. Status: {response.StatusCode}");
                }

                return await GetResponseBody<T>(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
                throw;
            }
        }

        private async Task<T> GetResponseBody<T>(HttpResponseMessage httpResponseMessage)
        {
            try
            {
                var response = await httpResponseMessage.Content.ReadAsStringAsync();
                var mediaType = httpResponseMessage.Content!.Headers!.ContentType!.MediaType;

                if (mediaType == ApplicationJson)
                {
                    var requestResult = JsonConvert.DeserializeObject<T>(response);
                    if (requestResult == null)
                    {
                        throw new HttpRequestException("Fail to convert result into json.");
                    }

                    return requestResult;
                }
                else if (mediaType == PlainText)
                {
                    if (response == null)
                    {
                        throw new HttpRequestException("Fail to convert result into text");
                    }

                    return (T)(object)response;
                }

                throw new Exception($"Operation Failed. Fail to convert the result. Response body is not in json or text format.");
            }
            catch
            {
                throw;
            }
        }
    }
}
