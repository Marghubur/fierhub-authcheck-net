using Bt.Ems.Lib.PipelineConfig.Model.ExceptionModel;
using fierhub_authcheck_net.Model;

namespace fierhub_authcheck_net.Middleware.Service
{
    public class FierhubServiceFilter(SessionDetail _session)
    {
        public void AuthorizationToken(Dictionary<string, string> mappedClaims)
        {
            if (mappedClaims != null)
            {
                LoadSession(mappedClaims);
            }
            else
            {
                throw EmstumException.Unauthorized("Authorization token not found.");
            }
        }

        public void LoadSession(Dictionary<string, string> claims)
        {
            _session.Claims = claims;

            claims.TryGetValue("fierhub_autogen_id", out string id);
            if (id != null)
            {
                _session.UserId = long.Parse(id);
            }

            claims.TryGetValue("fierhub_autogen_roles", out string roles);
            if (id != null)
            {
                _session.Roles = roles.Split(",").ToList<string>();
            }
        }
    }
}
