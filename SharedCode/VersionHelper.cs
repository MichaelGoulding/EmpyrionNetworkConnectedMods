using System;
using System.Linq;
using System.Reflection;

namespace SharedCode
{
    public static class VersionHelper
    {
        public static string GetVersionString(Type modType)
        {
            return (modType.GetTypeInfo().Assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute)).SingleOrDefault() as AssemblyTitleAttribute).Title;
        }
    }
}
