using System.Net;

namespace Fierhub.Service.Library.Model
{
    public class FierhubAuthResponse : FierhubResponse
    {
        public string AccessToken { get; set; } = null;
        public string RefreshToken { get; set; } = null;
        public string ErrorMessage { get; set; }
        public string ErrorCode { get; set; }

        public static FierhubAuthResponse Ok(dynamic Data, string Resion = null, string Token = null, string RefreshToken = null)
        {
            return new FierhubAuthResponse
            {
                AccessToken = Token,
                RefreshToken = RefreshToken,
                HttpStatusMessage = Resion,
                HttpStatusCode = HttpStatusCode.OK,
                ResponseBody = Data
            };
        }

        public static FierhubAuthResponse Build(dynamic Data, HttpStatusCode httpStatusCode, string Resion = null, string Token = null, string RefreshToken = null)
        {
            return new FierhubAuthResponse
            {
                AccessToken = Token,
                RefreshToken = RefreshToken,
                HttpStatusMessage = Resion,
                HttpStatusCode = httpStatusCode,
                ResponseBody = Data
            };
        }
    }
}
