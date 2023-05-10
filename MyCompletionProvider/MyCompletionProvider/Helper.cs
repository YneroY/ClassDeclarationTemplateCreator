using System.Collections.Generic;

namespace MyCompletionProvider
{
    public static class Helper
    {
        public static readonly Dictionary<string, string> PROPERTYTYPE_REFERENCE = new Dictionary<string, string>()
        {
            {"string", "\"\"" },
            {"int", "1" },
            {"long", "1" },
            {"short", "1" },
            {"double", "1.0" },
            {"bool", "true" },
            {"decimal", "1.00m" },
            {"System.DateTime", "DateTime.Now" }
        };
    }
}
