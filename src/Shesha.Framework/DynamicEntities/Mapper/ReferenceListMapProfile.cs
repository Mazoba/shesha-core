using Abp.Dependency;
using Abp.Reflection;
using Shesha.AutoMapper;
using Shesha.Extensions;
using Shesha.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Shesha.DynamicEntities.Mapper
{
    /// <summary>
    /// Entity mapping profile
    /// </summary>
    public class ReferenceListMapProfile : ShaProfile, ITransientDependency
    {
        private static List<ConverterInfo> Converters;

        static ReferenceListMapProfile() 
        {
            Converters = new List<ConverterInfo>();

            var typeFinder = StaticContext.IocManager.Resolve<ITypeFinder>();

            var reflistTypes = typeFinder.Find(t => t.IsReferenceListType()).ToList();
            foreach (var reflistType in reflistTypes)
            {
                //var numericType = Enum.GetUnderlyingType(reflistType);
                var numericType = typeof(Int64);

                Converters.Add(new ConverterInfo
                {
                    RefListType = reflistType,
                    NumericType = numericType,
                    NumericToEnumConverter = typeof(NumericToEnumTypeConverter<,>).MakeGenericType(numericType, reflistType),
                    EnumToNumericConverter = typeof(EnumToNumericTypeConverter<,>).MakeGenericType(reflistType, numericType),
                });
            }
        }

        public ReferenceListMapProfile()
        {
            foreach (var converterInfo in Converters)
            {
                CreateMap(converterInfo.NumericType, converterInfo.RefListType).ConvertUsing(converterInfo.NumericToEnumConverter);
                CreateMap(converterInfo.RefListType, converterInfo.NumericType).ConvertUsing(converterInfo.EnumToNumericConverter);
            }
        }

        public class ConverterInfo 
        { 
            public Type RefListType { get; set; }
            public Type NumericType { get; set; }
            public Type NumericToEnumConverter { get; set; }
            public Type EnumToNumericConverter { get; set; }
        }
    }
}
