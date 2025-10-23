namespace fierhub_authcheck_net.Model
{
    public class FierHubConfig
    {
        public string Token { get; set; }
        public bool IsDatabaseConfigurationEnable { get; set; }
        public bool IsApiGatewayEnable { get; set; }
        public string TokenConfigFileName { get; set; }
        public string DbConfigFileName { get; set; }
    }
}
