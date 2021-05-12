using System;
using System.Collections.Generic;
using System.Linq;
using Abp.Dependency;
using Abp.Reflection;
using Shesha.Domain.Attributes;
using Shesha.Extensions;
using Shesha.Reflection;
using Shesha.Utilities;

namespace Shesha.Configuration.Runtime
{
    /// <summary>
    /// Entity configuration store
    /// </summary>
    public class EntityConfigurationStore: IEntityConfigurationStore, ISingletonDependency
    {
        private readonly IDictionary<string, Type> _entityTypesByShortAlias = new Dictionary<string, Type>();
        private readonly IDictionary<Type, EntityConfiguration> _entityConfigurations = new Dictionary<Type, EntityConfiguration>();
        private readonly ITypeFinder _typeFinder;

        /// <summary>
        /// Entity types dictionary (key - TypeShortAlias, value - type of entity)
        /// </summary>
        public IDictionary<string, Type> EntityTypes => _entityTypesByShortAlias;

        public EntityConfigurationStore(ITypeFinder typeFinder)
        {
            _typeFinder = typeFinder;

            Initialise();
        }

        protected void Initialise()
        {
            var entityTypes = _typeFinder.FindAll().Where(t => t.IsEntityType())
                .Select(t => new { Type = t, TypeShortAlias = t.GetAttribute<EntityAttribute>()?.TypeShortAlias })
                .Where(i => !string.IsNullOrWhiteSpace(i.TypeShortAlias))
                .ToList();

            // check for duplicates
            var duplicates = entityTypes
                .GroupBy(i => i.TypeShortAlias, (t, items) => new {TypeShortAlias = t, Types = items.Select(i => i.Type)})
                .Where(g => g.Types.Count() > 1).ToList();
            if (duplicates.Any())
                throw new Exception($"Duplicated {nameof(EntityAttribute.TypeShortAlias)} found: {duplicates.Select(i => $"{i.TypeShortAlias}: { i.Types.Select(t => t.FullName) }").Delimited("; ")}");

            foreach (var entityType in entityTypes)
            {
                _entityTypesByShortAlias.Add(entityType.TypeShortAlias, entityType.Type);
            }
        }

        public Type GetEntityTypeFromAlias(string typeShortAlias)
        {
            return _entityTypesByShortAlias.ContainsKey(typeShortAlias)
                ? _entityTypesByShortAlias[typeShortAlias]
                : null;
        }

        public string GetEntityTypeAlias(Type entityType)
        {
            var entityConfig = Get(entityType);
            return entityConfig?.TypeShortAlias;
        }

        /// inheritedDoc
        public EntityConfiguration Get(string typeShortAlias)
        {
            if (!_entityTypesByShortAlias.ContainsKey(typeShortAlias))
                throw new Exception($"Entity with {nameof(EntityAttribute.TypeShortAlias)} = '{typeShortAlias}' not found");
            
            return Get(_entityTypesByShortAlias[typeShortAlias]);
        }

        /// inheritedDoc
        public EntityConfiguration Get(Type entityType)
        {
            var underlyingEntityType = entityType.StripCastleProxyType();

            if (!_entityConfigurations.TryGetValue(underlyingEntityType, out var config))
            {
                config = new EntityConfiguration(underlyingEntityType);
                lock (_entityConfigurations)
                {
                    if (!_entityConfigurations.TryGetValue(underlyingEntityType, out _))
                    {
                        _entityConfigurations.Add(underlyingEntityType, config);
                    }
                }
            }
            return config;
        }
    }
}
