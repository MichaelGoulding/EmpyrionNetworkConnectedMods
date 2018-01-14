using System;
using System.Linq;
using System.Reflection;

namespace EmpyrionModApi
{
    public static class Helpers
    {
        public static string GetVersionString(Type modType)
        {
            return (modType.GetTypeInfo().Assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute)).SingleOrDefault() as AssemblyTitleAttribute).Title;
        }

        public static T LoadFromYamlOrDefault<T>(string filePath) where T : class, new()
        {
            T result = null;

            if (System.IO.File.Exists(filePath))
            {
                using (var input = System.IO.File.OpenText(filePath))
                {
                    result = (new YamlDotNet.Serialization.Deserializer()).Deserialize<T>(input);
                }
            }

            return (result == null) ? new T() : result;
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
