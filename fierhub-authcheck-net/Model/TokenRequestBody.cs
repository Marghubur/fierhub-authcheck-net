namespace fierhub_authcheck_net.Model
{
    public class TokenRequestBody
    {
        public string? Key { get; set; }
        public string? Issuer { get; set; }
        public string? CompanyCode { get; set; }
        public int ExpiryTimeInSeconds { get; set; }
        public int RefreshTokenExpiryTimeInSeconds { get; set; }
        public Dictionary<string, object>? Claims { get; set; }
        public string? TokenName { get; set; }
        public Dictionary<string, string>? Roles { get; set; }
        public int ParentId { get; set; }
        public string? FileName { get; set; }
        public long RepositoryId { get; set; }
    }
}