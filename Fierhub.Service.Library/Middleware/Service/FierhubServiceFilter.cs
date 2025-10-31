using Fierhub.Service.Library.Model;

namespace Fierhub.Service.Library.Middleware.Service
{
    public class FierhubServiceFilter(UserSession _session)
    {
        public void StoreClaims(Dictionary<string, string> claims)
        {
            if (claims == null || claims.Count == 0)
            {
                throw new UnauthorizedAccessException("Not found any claims");
            }

            _session.Claims = claims;
        }

        public void MapClaims<T>(Dictionary<string, string> claims) where T : new()
        {
            _session.Claims = claims;
            T instance = new T();

            claims.TryGetValue("fierhub_autogen_id", out string id);
            if (id != null)
            {
                _session.UserId = long.Parse(id);
            }

            claims.TryGetValue("fierhub_autogen_roles", out string roles);
            if (id != null)
            {
                _session.Roles = roles.Split(",").ToList();
            }

            var properties = typeof(T).GetProperties();

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
                                prop.SetValue(instance, convertedValue);
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
