﻿using Abp.Domain.Entities;
using Newtonsoft.Json.Linq;
using Shesha.JsonLogic;
using Shesha.Services;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Shesha.Extensions
{
    /// <summary>
    /// Queryable extensions (https://stackoverflow.com/a/44071949)
    /// </summary>
    public static class IQueryableExtensions
    {
        public static IOrderedQueryable<T> OrderBy<T>(this IQueryable<T> query, string propertyName, IComparer<object> comparer = null)
        {
            return CallOrderedQueryable(query, "OrderBy", propertyName, comparer);
        }

        public static IOrderedQueryable<T> OrderByDescending<T>(this IQueryable<T> query, string propertyName, IComparer<object> comparer = null)
        {
            return CallOrderedQueryable(query, "OrderByDescending", propertyName, comparer);
        }

        public static IOrderedQueryable<T> ThenBy<T>(this IOrderedQueryable<T> query, string propertyName, IComparer<object> comparer = null)
        {
            return CallOrderedQueryable(query, "ThenBy", propertyName, comparer);
        }

        public static IOrderedQueryable<T> ThenByDescending<T>(this IOrderedQueryable<T> query, string propertyName, IComparer<object> comparer = null)
        {
            return CallOrderedQueryable(query, "ThenByDescending", propertyName, comparer);
        }

        /// <summary>
        /// Builds the Queryable functions using a TSource property name.
        /// </summary>
        public static IOrderedQueryable<T> CallOrderedQueryable<T>(this IQueryable<T> query, string methodName, string propertyName,
                IComparer<object> comparer = null)
        {
            var param = Expression.Parameter(typeof(T), "x");

            var body = propertyName.Split('.').Aggregate<string, Expression>(param, Expression.PropertyOrField);

            return comparer != null
                ? (IOrderedQueryable<T>)query.Provider.CreateQuery(
                    Expression.Call(
                        typeof(Queryable),
                        methodName,
                        new[] { typeof(T), body.Type },
                        query.Expression,
                        Expression.Lambda(body, param),
                        Expression.Constant(comparer)
                    )
                )
                : (IOrderedQueryable<T>)query.Provider.CreateQuery(
                    Expression.Call(
                        typeof(Queryable),
                        methodName,
                        new[] { typeof(T), body.Type },
                        query.Expression,
                        Expression.Lambda(body, param)
                    )
                );
        }

        /// <summary>
        /// Apply JsonLogic filter to a queryable. Note: it uses default <see cref="IJsonLogic2LinqConverter"/> registered in the IoCManager
        /// </summary>
        /// <param name="query">Queryable to be filtered</param>
        /// <param name="filter">String representation of JsonLogic filter</param>
        /// <returns></returns>
        public static IQueryable<TEntity> ApplyFilter<TEntity, TId>(this IQueryable<TEntity> query, string filter) where TEntity : class, IEntity<TId>
        {
            if (string.IsNullOrWhiteSpace(filter))
                return query;

            var jsonLogic = JObject.Parse(filter);

            var jsonLogicConverter = StaticContext.IocManager.Resolve<IJsonLogic2LinqConverter>();
            var expression = jsonLogicConverter.ParseExpressionOf<TEntity>(jsonLogic);

            return query.Where(expression);
        }
    }
}
