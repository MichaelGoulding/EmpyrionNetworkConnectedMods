using System;
using System.Linq;
using System.Reflection;

namespace SharedCode
{
    public static class Helpers
    {
        public static string GetVersionString(Type modType)
        {
            return (modType.GetTypeInfo().Assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute)).SingleOrDefault() as AssemblyTitleAttribute).Title;
        }

        public static void SaveAsYaml(string filePath, object obj)
        {
            using (var writer = System.IO.File.CreateText(filePath))
            {
                var serializer = new YamlDotNet.Serialization.Serializer();

                serializer.Serialize(writer, obj);
            }
        }
    }
}
