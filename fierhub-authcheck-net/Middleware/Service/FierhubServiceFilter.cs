using Bt.Ems.Lib.PipelineConfig.DbConfiguration.Model;
using Bt.Ems.Lib.PipelineConfig.Model.ExceptionModel;
using fierhub_authcheck_net.Model;

namespace fierhub_authcheck_net.Middleware.Service
{
    public class FierhubServiceFilter(SessionDetail _session, CurrentSession currentSession)
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

            var properties = typeof(CurrentSession).GetProperties();

            if (claims.Count > 0)
            {
                foreach (var prop in properties)
                {
                    if (claims.TryGetValue(prop.Name, out var value) && value != null)
                    {
                        try
                        {
                            object convertedValue = null;
                            if (prop.PropertyType == typeof(TimeZoneInfo))
                            {
                                var timeZone = TimeZoneInfo.GetSystemTimeZones()
                                    .FirstOrDefault(tz => tz.DisplayName.Equals(value, StringComparison.OrdinalIgnoreCase));

                                if (timeZone == null)
                                {
                                    try
                                    {
                                        timeZone = TimeZoneInfo.FindSystemTimeZoneById(value);
                                    }
                                    catch { /* ignore if invalid */ }
                                }

                                convertedValue = timeZone;
                            }
                            else
                            {
                                convertedValue = Convert.ChangeType(value, prop.PropertyType);
                            }

                            if (convertedValue != null)
                                prop.SetValue(currentSession, convertedValue);
                        }
                        catch
                        {
                            // Optional: handle conversion errors (e.g. type mismatch)
                        }
                    }
                }
            }
        }
    }
}
