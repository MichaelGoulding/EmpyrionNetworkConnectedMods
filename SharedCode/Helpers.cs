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

        public static T LoadFromYamlOrDefault<T>(string filePath) where T : new()
        {
            if (System.IO.File.Exists(filePath))
            {
                using (var input = System.IO.File.OpenText(filePath))
                {
                    return (new YamlDotNet.Serialization.Deserializer()).Deserialize<T>(input);
                }
            }
            else
            {
                return new T();
            }
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
