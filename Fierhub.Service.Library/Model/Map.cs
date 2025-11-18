namespace Fierhub.Service.Library.Model
{
    public static class Map
    {
        public static Dictionary<string, string> Of(params string[] items)
        {
            if (items.Length % 2 != 0)
                throw new ArgumentException("Number of arguments must be even (key/value pairs).");

            var dict = new Dictionary<string, string>();

            for (int i = 0; i < items.Length; i += 2)
            {
                dict[items[i]] = items[i + 1];
            }

            return dict;
        }
    }
}
