namespace Fierhub.Service.Library.Model
{
    public class UserSession
    {
        public long UserId { set; get; }
        public List<string> Roles { set; get; }
        public string Code { set; get; }
        public string LocalConnectionString { set; get; }
        public Dictionary<string, string> Claims { set; get; }
        public string Environment { get; set; }

        public T GetValue<T>(string key)
        {
            if (Claims == null)
            {
                throw new Exception("Claims are null or empty");
            }

            Claims!.TryGetValue(key, out var value);
            if (value == null)
            {
                return default;
            }

            return (T)Convert.ChangeType(value, typeof(T));
        }
    }
}
