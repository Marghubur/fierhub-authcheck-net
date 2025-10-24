using System.Net;

namespace Bt.Ems.Lib.PipelineConfig.DbConfiguration.Model.MicroserviceModel
{
    public class FierhubAuthResponse
    {
        public dynamic ResponseBody { get; set; }
        public string HttpStatusMessage { get; set; }
        public HttpStatusCode HttpStatusCode { get; set; }
        public string AuthenticationToken { get; set; } = null;

        public static FierhubAuthResponse Ok(dynamic Data, string Resion = null, string Token = null)
        {
            return new FierhubAuthResponse
            {
                AuthenticationToken = Token,
                HttpStatusMessage = Resion,
                HttpStatusCode = HttpStatusCode.OK,
                ResponseBody = Data
            };
        }

        public static FierhubAuthResponse Build(dynamic Data, HttpStatusCode httpStatusCode, string Resion = null, string Token = null)
        {
            return new FierhubAuthResponse
            {
                AuthenticationToken = Token,
                HttpStatusMessage = Resion,
                HttpStatusCode = httpStatusCode,
                ResponseBody = Data
            };
        }
    }
}
