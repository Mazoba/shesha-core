using Abp.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shesha.Specifications
{
    /// <summary>
    /// Provides access to a list of specifications that should be applied in current execution context. Includes both global specifications and custom ones (e.g. applied to concrete API endpoints)
    /// </summary>
    public interface ISpecificationManager
    {
        /// <summary>
        /// List of specifications in current execution context
        /// </summary>
        List<Type> SpecificationTypes { get; }

        /// <summary>
        /// Apply all specifications of the current context
        /// </summary>
        /// <typeparam name="T">Type of entity</typeparam>
        /// <param name="queryable">Queryable to apply specifications</param>
        /// <returns></returns>
        IQueryable<T> ApplySpecifications<T>(IQueryable<T> queryable);

        /// <summary>
        /// Get active specification from current context
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        List<ISpecification<T>> GetSpecifications<T>();

        /// <summary>
        /// Activate specifications context
        /// </summary>
        /// <typeparam name="TSpec">Type of specifications</typeparam>
        /// <typeparam name="TEntity">Type of entity</typeparam>
        /// <returns></returns>
        ISpecificationsContext Use<TSpec, TEntity>() where TSpec : ISpecification<TEntity>;

        /// <summary>
        /// Activate specifications context
        /// </summary>
        /// <param name="specificationType">Type of specifications</param>
        IDisposable Use(params Type[] specificationType);

        /// <summary>
        /// Disables all specifications activate using current specifications manager
        /// </summary>
        /// <returns></returns>
        IDisposable DisableSpecifications();
    }
}
