using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Linq.Expressions;
using Abp.Domain.Entities;
using AutoMapper;
using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using Shesha.AutoMapper.Dto;
using Shesha.Extensions;
using Shesha.Reflection;
using Shesha.Services;

namespace Shesha.AutoMapper
{
    public static class AutoMapperExtensions
    {
        public static IMappingExpression<TSource, TDestination> IgnoreNotMapped<TSource, TDestination>(
               this IMappingExpression<TSource, TDestination> expression)
        {
            var sourceType = typeof(TSource);

            foreach (var property in sourceType.GetProperties())
            {
                if (property.HasAttribute<NotMappedAttribute>())
                    expression.ForMember(property.Name, opt => opt.Ignore());
            }

            return expression;
        }

        public static IMappingExpression<TSource, TDestination> IgnoreDestinationChildEntities<TSource, TDestination>(
            this IMappingExpression<TSource, TDestination> expression)
        {
            var destinationType = typeof(TDestination);

            foreach (var property in destinationType.GetProperties())
            {
                if (typeof(IEntity).IsAssignableFrom(property.PropertyType))
                    expression.ForMember(property.Name, opt => opt.Ignore());
            }
            return expression;
        }

        public static IMappingExpression<TSource, TDestination> IgnoreDestinationChildEntityLists<TSource, TDestination>(
            this IMappingExpression<TSource, TDestination> expression)
        {
            var destinationType = typeof(TDestination);

            foreach (var property in destinationType.GetProperties())
            {
                if (property.PropertyType.IsSubtypeOfGeneric(typeof(IList<>)))
                {
                    var genericArgument = property.PropertyType.GenericTypeArguments.FirstOrDefault();

                    if (genericArgument?.GetInterfaces().Contains(typeof(IEntity)) == true)
                        expression.ForMember(property.Name, opt => opt.Ignore());
                }
            }
            return expression;
        }

        public static IMappingExpression<TSource, TDestination> IgnoreChildEntities<TSource, TDestination>(
            this IMappingExpression<TSource, TDestination> expression)
        {
            var sourceType = typeof(TSource);
            var destinationType = typeof(TDestination);

            foreach (var property in sourceType.GetProperties())
            {
                if (typeof(IEntity).IsAssignableFrom(property.PropertyType) && destinationType.GetProperty(property.Name) != null)
                {
                    expression.ForMember(property.Name, opt => opt.Ignore());
                }
            }
            return expression;
        }

        public static IMappingExpression<TSource, TDestination> IgnoreChildEntityLists<TSource, TDestination>(
               this IMappingExpression<TSource, TDestination> expression)
        {
            var sourceType = typeof(TSource);

            foreach (var property in sourceType.GetProperties())
            {
                if (property.PropertyType.IsSubtypeOfGeneric(typeof(IList<>)))
                {
                    var genericArgument = property.PropertyType.GenericTypeArguments.FirstOrDefault();

                    if (genericArgument?.GetInterfaces().Contains(typeof(IEntity)) == true)
                        expression.ForMember(property.Name, opt => opt.Ignore());
                }
            }
            return expression;
        }

        /// <summary>
        /// Maps all reference list properties of the source type to <see cref="ReferenceListItemValueDto"/> of the destination type
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TDestination"></typeparam>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static IMappingExpression<TSource, TDestination> MapReferenceListValuesToDto<TSource, TDestination>(
            this IMappingExpression<TSource, TDestination> expression)
        {
            var sourceType = typeof(TSource);
            var destinationType = typeof(TDestination);

            var refListProperties = destinationType.GetProperties().Where(p => p.PropertyType == typeof(ReferenceListItemValueDto))
                .Select(d =>
                    {
                        var source = sourceType.GetProperty(d.Name);
                        return new
                        {
                            DstProperty = d,
                            SrcProperty = source,
                            RefListIdentifier = source?.GetReferenceListIdentifierOrNull()
                        };
                    }
                )
                .Where(i => i.RefListIdentifier != null)
                .ToList();

            foreach (var item in refListProperties)
            {
                expression.ForMember(item.DstProperty.Name, m => m.MapFrom(e => e != null
                    ? GetRefListItemValueDto(item.RefListIdentifier.Namespace, item.RefListIdentifier.Name, item.SrcProperty.GetValue(e))
                    : null));
            }

            return expression;
        }

        private static ReferenceListItemValueDto GetRefListItemValueDto(string refListNamespace, string refListName, object value)
        {
            var intValue = value != null
                ? Convert.ToInt32(value)
                : (int?)null;

            return intValue != null
                ? new ReferenceListItemValueDto
                {
                    ItemValue = intValue.Value,
                    Item = GetRefListItemText(refListNamespace, refListName, intValue.Value)
                }
                : null;
        }

        private static string GetRefListItemText(string refListNamespace, string refListName, int? value)
        {
            if (value == null)
                return null;
            var helper = StaticContext.IocManager.Resolve<IReferenceListHelper>();
            return helper.GetItemDisplayText(refListNamespace, refListName, value);
        }

        /// <summary>
        /// Maps all <see cref="ReferenceListItemValueDto"/> properties of the source type to reference list values in the destination type
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TDestination"></typeparam>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static IMappingExpression<TSource, TDestination> MapReferenceListValuesFromDto<TSource, TDestination>(
            this IMappingExpression<TSource, TDestination> expression)
        {
            var sourceType = typeof(TSource);
            var destinationType = typeof(TDestination);

            var refListProperties = sourceType.GetProperties().Where(p => p.PropertyType == typeof(ReferenceListItemValueDto))
                .Select(p =>
                    {
                        var destination = destinationType.GetProperty(p.Name);

                        var propType = destination?.PropertyType.GetUnderlyingTypeIfNullable();
                        if (propType == null || propType != typeof(int) && !propType.IsEnum)
                            return null;

                        return new
                        {
                            DstProperty = destination,
                            SrcProperty = p,
                            PropType = propType
                        };
                    }
                )
                .Where(i => i != null)
                .ToList();
            
            foreach (var item in refListProperties)
            {
                expression.ForMember(item.DstProperty.Name, m => m.MapFrom(e => e != null ? GetRefListItemValue(item.SrcProperty.GetValue(e) as ReferenceListItemValueDto, item.SrcProperty.PropertyType) : null));
            }

            return expression;
        }
        
        /*
        /// <summary>
        /// Map child entity to EntityWithDisplayNameDto
        /// </summary>
        public static IMappingExpression<TSrc, TDest> MapEntityWithDisplayName<TSrc, TDest, TEntity, TId, TDto>(
            this IMappingExpression<TSrc, TDest> expression, Expression<Func<TDest, TDto>> propFunc, Func<TSrc, TEntity> memberExpression) 
            where TEntity : IEntity<TId> 
            where TDto : EntityWithDisplayNameDto<TId>, new()
        {
            return expression.ForMember(propFunc,
                    options => options.MapFrom(e => GetEntityWithDisplayNameDto<TSrc, TEntity, TId, TDto>(memberExpression, e)));
        }

        private static TDto GetEntityWithDisplayNameDto<TSrc, TEntity, TId, TDto>(Func<TSrc, TEntity> propFunc, TSrc e)
            where TEntity : IEntity<TId> 
            where TDto : EntityWithDisplayNameDto<TId>, new()
        {
            var propValue = propFunc.Invoke(e);
            return propValue != null
                ? new TDto() {Id = propValue.Id, DisplayText = propValue.GetDisplayName()}
                : null;
        }

        /// <summary>
        /// Map child entity to EntityWithDisplayNameDto
        /// </summary>
        public static IMappingExpression<TSrc, TDest> MapEntityWithDisplayNameNullable<TSrc, TDest, TEntity, TId, TDto>(
            this IMappingExpression<TSrc, TDest> expression, Expression<Func<TDest, TDto>> propFunc, Func<TSrc, TEntity> memberExpression)
            where TEntity : IEntity<TId>
            where TId: struct
            where TDto : EntityWithDisplayNameDto<TId?>, new()
        {
            return expression.ForMember(propFunc,
                options => options.MapFrom(e => GetEntityWithDisplayNameDtoNullable<TSrc, TEntity, TId, TDto>(memberExpression, e)));
        }

        private static TDto GetEntityWithDisplayNameDtoNullable<TSrc, TEntity, TId, TDto>(Func<TSrc, TEntity> propFunc, TSrc e)
            where TEntity : IEntity<TId>
            where TId : struct
            where TDto : EntityWithDisplayNameDto<TId?>, new()
        {
            var propValue = propFunc.Invoke(e);
            return propValue != null
                ? new TDto() { Id = propValue.Id, DisplayText = propValue.GetDisplayName() }
                : null;
        }
        */

        private static object GetRefListItemValue(ReferenceListItemValueDto dto, Type propType)
        {
            if (dto?.ItemValue == null)
                return null;

            if (propType.IsEnum)
                return Enum.ToObject(propType, dto.ItemValue);

            return propType == typeof(byte)
                ? Convert.ToByte(dto.ItemValue.Value)
                : propType == typeof(Int64)
                    ? Convert.ToInt64(dto.ItemValue.Value)
                    : dto.ItemValue.Value;
        }
    }
}
