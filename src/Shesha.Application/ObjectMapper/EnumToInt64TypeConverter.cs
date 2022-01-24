using AutoMapper;

namespace Shesha.ObjectMapper
{
    /// <summary>
    /// Enum to Int64 cnoverter
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EnumToInt64TypeConverter<T> : ITypeConverter<T, long> where T : struct
    {
        public long Convert(T source, long destination, ResolutionContext context)
        {
            return System.Convert.ToInt64(source);
        }
    }
}
