namespace Fierhub.Service.Library.Model
{
    public class SessionDetail
    {
        public long UserId { set; get; }
        public List<string> Roles { set; get; }
        public string Code { set; get; }
        public string LocalConnectionString { set; get; }
        public Dictionary<string, string> Claims { set; get; }
        public string Environment { get; set; }        
    }
}
