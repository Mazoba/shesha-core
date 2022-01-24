using AutoMapper;
using System;

namespace Shesha.ObjectMapper
{
    /// <summary>
    /// Int64 to enum converter
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Int64ToEnumTypeConverter<T> : ITypeConverter<long, T> where T : struct
    {
        public T Convert(long source, T destination, ResolutionContext context)
        {
            return (T)Enum.ToObject(typeof(T), source);
        }
    }
}
