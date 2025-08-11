namespace fierhub_authcheck_net.Model
{
    public class ResponseModel
    {
        public string? message { get; set; }
        public int statusCode { get; set; }
        public object? responseBody { get; set; }
    }
}
