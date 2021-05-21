using System;

namespace Shesha.Extensions
{
    public static class TypeExtensions
    {
        public static object GetTypeDefaultValue(this Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            return
                type.IsValueType
                    ? Activator.CreateInstance(type) //value type
                    : null; //reference type
        }
    }
}
