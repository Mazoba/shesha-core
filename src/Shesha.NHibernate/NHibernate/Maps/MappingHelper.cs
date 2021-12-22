﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using Abp.Domain.Entities;
using NHibernate.Mapping.ByCode;
using PluralizeService.Core;
using Shesha.Domain;
using Shesha.Domain.Attributes;
using Shesha.Domain.Interfaces;
using Shesha.Extensions;
using Shesha.Reflection;

namespace Shesha.NHibernate.Maps
{
    public static class NhMappingHelper
    {
        /// <summary>
        /// Returns true if the property is persisted to the DB
        /// </summary>
        /// <param name="prop"></param>
        /// <returns></returns>
        public static bool IsPersistentProperty(MemberInfo prop)
        {
            if (prop.HasAttribute<NotMappedAttribute>())
                return false;

            if (!MappingHelper.IsRootEntity(prop.DeclaringType) && prop.DeclaringType.BaseType != null)
            {
                var upperLevelProperty = prop.DeclaringType.BaseType.GetProperty(prop.Name);
                if (upperLevelProperty != null)
                {
                    if (MappingHelper.GetColumnName(prop) == MappingHelper.GetColumnName(upperLevelProperty))
                        return false;
                }
            }

            var inspector = new SimpleModelInspector() as IModelInspector;
            return inspector.IsPersistentProperty(prop);
        }
    }
}
