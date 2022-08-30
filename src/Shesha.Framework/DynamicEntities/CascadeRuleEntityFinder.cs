using Abp.Dependency;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shesha.DynamicEntities
{
    public interface ICascadeRuleEntityFinder
    {
        IIocManager IocManager { get; set; }
        object FindEntity(CascadeRuleEntityFinderInfo change);
    }

    public abstract class CascadeRuleEntityFinderBase<T, TId> : ICascadeRuleEntityFinder where T : class, IEntity<TId>
    {
        public IIocManager IocManager { get; set; }

        public object FindEntity(CascadeRuleEntityFinderInfo info)
        {
            var newInfo = new CascadeRuleEntityFinderInfo<T, TId>((T)info._NewObject)
            {
                _Repository = (IRepository<T, TId>)info._Repository ?? IocManager.Resolve<IRepository<T, TId>>(),
            };

            return FindEntity(newInfo);
        }

        /// <summary>
        /// Override this function to check input and find Entity
        /// </summary>
        /// <param name="info">Input data</param>
        /// <returns>Found Entity. Null if not found. Throw exception <see cref="CascadeUpdateRuleException"/> if found any constraints</returns>
        /// <exception cref="CascadeUpdateRuleException">Throw exception of this type if found any constraints</exception>
        public virtual T FindEntity(CascadeRuleEntityFinderInfo<T, TId> info)
        {
            throw new NotImplementedException();
        }
    }

    public class CascadeRuleEntityFinderInfo
    {
        public CascadeRuleEntityFinderInfo(object newObject)
        {
            _NewObject = newObject;
        }

        public object _NewObject { get; set; }
        public IRepository _Repository { get; set; }
    }

    public class CascadeRuleEntityFinderInfo<T, TId> : CascadeRuleEntityFinderInfo where T : class, IEntity<TId>
    {
        public CascadeRuleEntityFinderInfo(T newObject) : base(newObject)
        {
        }

        public T NewObject => (T)_NewObject;
        public IRepository<T, TId> Repository => (IRepository<T, TId>)_Repository;
    }

    /// <summary>
    /// Throw exception of this type if found any constraints
    /// </summary>
    public class CascadeUpdateRuleException : Exception
    {
        public CascadeUpdateRuleException(string message) : base(message)
        {

        }
    }

}
