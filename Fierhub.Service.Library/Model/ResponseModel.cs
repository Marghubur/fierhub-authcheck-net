namespace Fierhub.Service.Library.Model
{
    public class ResponseModel
    {
        public string message { get; set; }
        public int statusCode { get; set; }
        public object responseBody { get; set; }
    }
}
