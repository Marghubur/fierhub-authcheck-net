using Bt.Ems.Lib.PipelineConfig.DbConfiguration.Model;
using Bt.Ems.Lib.PipelineConfig.Model.ExceptionModel;

namespace fierhub_authcheck_net.Model
{
    public class SessionDetail
    {
        public long UserId { set; get; }
        public List<string> Roles { set; get; }
        public string Code { set; get; }
        public string LocalConnectionString { set; get; }
        public Dictionary<string, string> Claims { set; get; }
        public DefinedEnvironments Environment { get; set; }
        public T GetValue<T>(string key)
        {
            if (Claims == null)
            {
                throw EmstumException.BadRequest("Claims are null or empty");
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
