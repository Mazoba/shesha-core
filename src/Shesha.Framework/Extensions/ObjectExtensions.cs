﻿using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Abp.Domain.Entities;
using Shesha.Domain;
using Shesha.Domain.Attributes;
using Shesha.Reflection;

namespace Shesha.Extensions
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Indicates is an entity
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool IsEntity(this object obj)
        {
            return obj?.GetType().IsEntityType() ?? false;
        }

        /// <summary>
        /// Indicates is the specified type a type of entity
        /// </summary>
        public static bool IsEntityType(this Type type)
        {
            return type != null &&
                   !type.IsAbstract &&
                   !type.IsGenericType &&
                   !type.HasAttribute<NotMappedAttribute>() &&
                   (type.GetInterfaces().Contains(typeof(IEntity))
                    || type.GetInterfaces().Any(x =>
                        x.IsGenericType &&
                        x.GetGenericTypeDefinition() == typeof(IEntity<>)));
        }

        /// <summary>
        /// Indicates is the specified type a reference list type
        /// </summary>
        public static bool IsReferenceListType(this Type type)
        {
            return type != null && type.IsPublic && type.IsEnum && type.HasAttribute<ReferenceListAttribute>();
        }

        /// <summary>
        /// Get type of the `Id` property. Applicable for entity types
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Type GetEntityIdType(this Type type) 
        {
            return type?.GetProperty(SheshaDatabaseConsts.IdColumn)?.PropertyType;
        }
    }
}
