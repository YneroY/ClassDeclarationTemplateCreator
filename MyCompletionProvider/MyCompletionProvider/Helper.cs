using System.Collections.Generic;

namespace MyCompletionProvider
{
    public static class Helper
    {
        public static readonly Dictionary<string, string> PROPERTYTYPE_REFERENCE = new Dictionary<string, string>()
        {
            {"String", "\"\"" },
            {"Int64", "1" },
            {"Int32", "1" },
            {"Boolean", "true" },
            {"decimal", "1.00m" },
            {"List", "new List<T>()" },
        };
    }
}
