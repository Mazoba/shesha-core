using AutoMapper;
using System;

namespace Shesha.ObjectMapper
{
    /// <summary>
    /// Enum to numeric converter
    /// </summary>
    public class EnumToNumericTypeConverter<TSource, TDestination> : ITypeConverter<TSource, TDestination> where TSource : Enum
    {
        public TDestination Convert(TSource source, TDestination destination, ResolutionContext context)
        {
            return (TDestination)System.Convert.ChangeType(source, typeof(TDestination));
        }
    }
}
