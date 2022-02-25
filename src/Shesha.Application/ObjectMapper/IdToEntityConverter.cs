using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using AutoMapper;
using Shesha.Services;
using System;

namespace Shesha.ObjectMapper
{
    /// <summary>
    /// Id to Entity cnoverter
    /// </summary>
    public class IdToEntityConverter<TEntity, TId> : ITypeConverter<TId, TEntity> where TEntity: class, IEntity<TId>
    {
        public TEntity Convert(TId source, TEntity destination, ResolutionContext context)
        {
            if (source == null || (source is Guid guid) && guid == Guid.Empty)
                return null;

            var repository = StaticContext.IocManager.Resolve<IRepository<TEntity, TId>>();

            return repository.Get(source);
        }
    }
}
