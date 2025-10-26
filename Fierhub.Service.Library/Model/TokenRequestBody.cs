namespace Fierhub.Service.Library.Model
{
    public class TokenRequestBody
    {
        public string Key { get; set; }
        public string Issuer { get; set; }
        public string Code { get; set; }
        public string Id { get; set; }
        public int ExpiryTimeInSeconds { get; set; }
        public int RefreshTokenExpiryTimeInSeconds { get; set; }
        public bool IsPrimary { get; set; }
        public Dictionary<string, string> Claims { get; set; }
    }
}