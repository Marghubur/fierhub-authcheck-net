namespace fierhub_authcheck_net.Model
{
    public class DatabaseProperties
    {
        public string OrganizationCode { get; set; }
        public string Code { get; set; }
        public string NodeId { get; set; }
        public string Schema { get; set; }
        public string DatabaseName { get; set; }
        public string Server { get; set; }
        public string Port { get; set; }
        public string Database { get; set; }
        public string UserId { get; set; }
        public string Password { get; set; }
        public int ConnectionTimeout { get; set; }
        public int ConnectionLifetime { get; set; }
        public int MinPoolSize { get; set; }
        public int MaxPoolSize { get; set; }
        public bool Pooling { get; set; }
        public string BuildConnectionString()
        {
            return $"server={Server};port={Port};database={Database};User Id={UserId};password={Password};Connection Timeout={ConnectionTimeout};Connection Lifetime={ConnectionLifetime};Min Pool Size={MinPoolSize};Max Pool Size={MaxPoolSize};Pooling={Pooling};";
        }
    }
}
