using System.Reflection;

namespace IgnisEducationSuite.Client.Services
{
    public static class checknullValues
    {
        public static bool HasNullNonVirtualProperties<T>(this T obj) where T : class
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj), "The object cannot be null.");

            return obj.GetType()
                      .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                      .Where(p => p.CanRead && !p.GetMethod.IsVirtual) // Exclude virtual properties
                      .Any(p => p.GetValue(obj) == null);
        }
        public static bool HasNullProperties<T>(this T obj) where T : class
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj), "The object cannot be null.");

            return obj.GetType()
                      .GetProperties()
                      .Where(p => p.CanRead) // Ensure the property is readable
                      .Any(p => p.GetValue(obj) == null);
        }
    }
}
