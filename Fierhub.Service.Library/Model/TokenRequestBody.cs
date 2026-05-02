namespace Fierhub.Service.Library.Model
{
    public class TokenRequestBody
    {
        public string Key { get; set; }
        public string Issuer { get; set; }
        public string Code { get; set; }
        public string Sid { get; set; }
        public string Device { get; set; }
        public string Audiance { set; get; }
        public double ExpiryTimeInSeconds { get; set; }
        public double RefreshTokenExpiryTimeInSeconds { get; set; }
        public bool IsPrimary { get; set; }
        public Dictionary<string, string> Claims { get; set; }
    }
}