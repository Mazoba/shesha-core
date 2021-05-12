using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Uow;
using NHibernate;
using Shesha.Configuration.Runtime;
using Shesha.Domain.Attributes;
using Shesha.NHibernate.UoW;
using Shesha.Utilities;

namespace Shesha.Services
{
    /// <summary>
    /// Dynamic repository
    /// </summary>
    public class DynamicRepository: IDynamicRepository, ITransientDependency
    {
        private readonly IEntityConfigurationStore _entityConfigurationStore;
        private readonly ICurrentUnitOfWorkProvider _currentUoqProvider;

        // note: current session doesn't work in unit tests because of static context usage
        private ISession CurrentSession => _currentUoqProvider.Current is NhUnitOfWork nhUow
            ? nhUow.Session
            : null;

        public DynamicRepository(IEntityConfigurationStore entityConfigurationStore, ICurrentUnitOfWorkProvider currentUoqProvider)
        {
            _entityConfigurationStore = entityConfigurationStore;
            _currentUoqProvider = currentUoqProvider;
        }
        
        /// <inheritdoc/>
        public async Task<object> GetAsync(string entityTypeShortAlias, string id)
        {
            var entityConfiguration = _entityConfigurationStore.Get(entityTypeShortAlias);
            if (entityConfiguration == null)
                throw new Exception($"Failed to get a configuration of an entity with {nameof(EntityAttribute.TypeShortAlias)} = '{entityTypeShortAlias}'");
            return await GetAsync(entityConfiguration.EntityType, id);
        }

        /// <inheritdoc/>
        public object Get(string entityTypeShortAlias, string id)
        {
            var entityConfiguration = _entityConfigurationStore.Get(entityTypeShortAlias);
            if (entityConfiguration == null)
                throw new Exception($"Failed to get a configuration of an entity with {nameof(EntityAttribute.TypeShortAlias)} = '{entityTypeShortAlias}'");
            return Get(entityConfiguration.EntityType, id);
        }

        /// <inheritdoc/>
        public async Task<object> GetAsync(Type entityType, string id)
        {
            var parsedId = Parser.ParseId(id, entityType);
            var session = CurrentSession;
            return await session.GetAsync(entityType, parsedId);
        }

        /// <inheritdoc/>
        public object Get(Type entityType, string id)
        {
            var parsedId = Parser.ParseId(id, entityType);
            var session = CurrentSession;
            return session.Get(entityType, parsedId);
        }

        /// <inheritdoc/>
        public async Task SaveOrUpdateAsync(object entity)
        {
            var session = CurrentSession;
            await session.SaveOrUpdateAsync(entity);
        }

        /// <inheritdoc/>
        public IQueryable<T> Query<T>()
        {
            return CurrentSession.Query<T>();
        }
    }
}
