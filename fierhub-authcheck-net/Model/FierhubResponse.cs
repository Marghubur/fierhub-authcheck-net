using System.Net;

namespace fierhub_authcheck_net.Model
{
    public class FierhubResponse
    {
        public dynamic ResponseBody { get; set; }
        public string HttpStatusMessage { get; set; }
        public HttpStatusCode HttpStatusCode { get; set; }

        public static FierhubResponse Ok(dynamic Data, string Resion = null, string Token = null)
        {
            return new FierhubResponse
            {
                HttpStatusMessage = Resion,
                HttpStatusCode = HttpStatusCode.OK,
                ResponseBody = Data
            };
        }

        public static FierhubResponse Build(dynamic Data, HttpStatusCode httpStatusCode, string Resion = null, string Token = null)
        {
            return new FierhubResponse
            {
                HttpStatusMessage = Resion,
                HttpStatusCode = httpStatusCode,
                ResponseBody = Data
            };
        }
    }
}
