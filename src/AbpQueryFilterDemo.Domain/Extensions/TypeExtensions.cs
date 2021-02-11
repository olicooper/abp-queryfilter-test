using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbpQueryFilterDemo.Extensions
{
    public static class TypeExtensions
    {
        /// <summary>
        /// See original: https://stackoverflow.com/a/56756010/2634818
        /// </summary>
        /// <returns>Friendly name including generic type parameters</returns>
        public static string GetFriendlyName(this Type type)
        {
            if (type.IsGenericType)
            {
                return GetTypeString(type);
            }
            return type.FullName;
        }

        private static string GetTypeString(Type type)
        {
            var t = type.AssemblyQualifiedName;

            var output = new StringBuilder();
            List<string> typeStrings = new List<string>();

            int iAssyBackTick = t.IndexOf('`') + 1;
            output.Append(t.Substring(0, iAssyBackTick - 1).Replace("[", string.Empty));
            var genericTypes = type.GetGenericArguments();

            foreach (var genType in genericTypes)
            {
                typeStrings.Add(genType.IsGenericType ? GetTypeString(genType) : genType.ToString());
            }

            output.Append($"<{string.Join(",", typeStrings)}>");
            return output.ToString();
        }
    }
}
