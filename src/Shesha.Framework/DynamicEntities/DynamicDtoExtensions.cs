using Shesha.DynamicEntities.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shesha.DynamicEntities
{
    public static class DynamicDtoExtensions
    {
        /// <summary>
        /// Returns true if the specified <paramref name="type"/> implements <see cref="IDynamicDto{TEntity, TId}"/>
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsDynamicDto(this Type type) 
        {
            return type.GetInterfaces().Any(x =>
                x.IsGenericType &&
                x.GetGenericTypeDefinition() == typeof(IDynamicDto<,>));
        }

        /// <summary>
        /// Returns an entity type of the specified dynamic DTO (<see cref="IDynamicDto{TEntity, TId}"/>)
        /// </summary>
        /// <param name="type">Type of the dynamic DTO, <see cref="IDynamicDto{TEntity, TId}"/></param>
        /// <returns></returns>
        public static Type GetDynamicDtoEntityType(Type type)
        {
            if (!type.IsDynamicDto())
                return null;

            var arguments = type.GetGenericArguments();
            return arguments.FirstOrDefault();
        }
    }
}
