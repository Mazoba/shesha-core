using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using Abp.Dependency;
using NHibernate;
using NHibernate.Persister.Entity;
using Shesha.Domain;
using Shesha.Domain.Attributes;
using Shesha.Extensions;
using Shesha.Reflection;
using Shesha.Services;

namespace Shesha.Configuration.Runtime
{
    /// <summary>
    /// Provides configuration information on Domain Model entities
    /// required at Run-time.
    /// </summary>
    public class EntityConfiguration
    {
        private static IDictionary<Type, EntityConfiguration> _entityConfigurations =
            new Dictionary<Type, EntityConfiguration>();

        private string _typeShortAlias;

        #region Constructors and Initialisation

        public EntityConfiguration(Type entityType)
        {
            entityType = entityType.StripCastleProxyType();
            EntityType = entityType;
            Properties = new Dictionary<string, PropertyConfiguration>();

            var configByReflectionLoader = new EntityConfigurationLoaderByReflection();
            configByReflectionLoader.LoadConfiguration(this);
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// The name of the property that will be used to display the entity to the user.
        /// </summary>
        public PropertyInfo DisplayNamePropertyInfo { get; set; }
        public PropertyInfo InactiveFlagPropertyInfo { get; set; }
        public PropertyInfo InactivateTimestampPropertyInfo { get; set; }
        public PropertyInfo InactivateUserPropertyInfo { get; set; }
        public PropertyInfo InactivateReasonPropertyInfo { get; set; }
        public PropertyInfo CreatedUserPropertyInfo { get; set; }
        public PropertyInfo CreatedTimestampPropertyInfo { get; set; }
        public PropertyInfo LastUpdatedUserPropertyInfo { get; set; }
        public PropertyInfo LastUpdatedTimestampPropertyInfo { get; set; }
        public PropertyInfo TestObjectFlagPropertyInfo { get; set; }
        public PropertyInfo DefaultSortOrderPropertyInfo { get; set; }

        public bool HasTypeShortAlias => !string.IsNullOrEmpty(_typeShortAlias);

        public string SafeTypeShortAlias => _typeShortAlias;

        /// <summary>
        /// Type short alias of the entity type (see <see cref="EntityAttribute"/>)
        /// </summary>
        /// <exception cref="ConfigurationErrorsException">Thrown when entity has no TypeShortAlias</exception>
        public string TypeShortAlias
        {
            get
            {
                if (string.IsNullOrEmpty(_typeShortAlias))
                    throw new ConfigurationErrorsException(
                        $"TypeShortAlias property for entity '{EntityType.FullName}' has not been specified.");

                const int typeShortAliasMaxLength = 50;
                if (!string.IsNullOrEmpty(_typeShortAlias) && _typeShortAlias.Length > typeShortAliasMaxLength)
                    throw new ConfigurationErrorsException(
                        $"TypeShortAlias property for entity '{EntityType.FullName}' must be {typeShortAliasMaxLength} characters or less.");

                return _typeShortAlias;
            }
            set => _typeShortAlias = value;
        }
        public string FriendlyName { get; set; }
        public string ControllerName { get; set; }
        public string DrillToView { get; set; }

        /// <summary>
        /// The view that should be used by default to view the details of the entity.
        /// </summary>
        public string DetailsActionName { get; set; } = "Details";
        public string CreateActionName { get; set; } = "Create";
        public string EditActionName { get; set; } = "Edit";
        public string InactivateActionName { get; set; } = "Inactivate";
        public string DeleteActionName { get; set; } = "Delete";

        public string TableName => MappingMetadata?.TableName;
        public string DiscriminatorValue => MappingMetadata?.DiscriminatorValue;

        private EntityMappingMetadata _mappingMetadata;
        private static object _nhMetadataLock = new object();
        public EntityMappingMetadata MappingMetadata
        {
            get
            {
                if (_mappingMetadata != null)
                    return _mappingMetadata;

                lock (_nhMetadataLock)
                {
                    if (_mappingMetadata != null)
                        return _mappingMetadata;

                    var sessionFactory = StaticContext.IocManager.Resolve<ISessionFactory>();
                    var persister = sessionFactory.GetClassMetadata(EntityType) as SingleTableEntityPersister;

                    var mappingMetadata = new EntityMappingMetadata()
                    {
                        TableName = persister?.TableName,
                        DiscriminatorValue = persister?.DiscriminatorSQLValue,
                        IsMultiTable = persister?.IsMultiTable ?? false,
                    };
                    mappingMetadata.SubclassTableName = mappingMetadata.IsMultiTable
                        ? persister?.GetSubclassTableName(1)
                        : null;

                    return _mappingMetadata = mappingMetadata;
                }
            }
        }

        private readonly object _typeShortAliasesHierarchyLock = new object();
        private List<string> _typeShortAliasesHierarchy;
        public List<string> TypeShortAliasesHierarchy
        {
            get
            {
                if (_typeShortAliasesHierarchy != null)
                    return _typeShortAliasesHierarchy;

                lock (_typeShortAliasesHierarchyLock)
                {
                    var result = new List<string>();
                    if (HasTypeShortAlias)
                        result.Add(TypeShortAlias);

                    var type = EntityType.BaseType;
                    while (type != null && !type.IsAbstract)
                    {
                        var config = type.GetEntityConfiguration();
                        if (config.HasTypeShortAlias)
                            result.Add(config.TypeShortAlias);
                        type = type.BaseType;
                    }
                    result = result.Where(i => !string.IsNullOrWhiteSpace(i)).Distinct().ToList();
                    _typeShortAliasesHierarchy = result;
                }

                return _typeShortAliasesHierarchy;
            }
        }

        public Type EntityType { get; set; }
        public Type IdType => EntityType?.GetEntityIdType();

        /// <summary>
        /// Gets whether the repository supports the inactivation of objects. When an
        /// object is inactivated it is not deleted but flagged in the database as inactive and 
        /// prevented from being returned to any calling application by default.
        /// </summary>
        public bool SupportsInactivate => InactiveFlagPropertyInfo != null;

        public IList<PropertySetChangeLoggingConfiguration> ChangeLogConfigurations = new List<PropertySetChangeLoggingConfiguration>();
        public bool HasPropertiesNeedToLogChangesFor { get; set; }

        public Dictionary<string, PropertyConfiguration> Properties { get; set; }

        public PropertyConfiguration this[string propertyName]
        {
            get
            {
                if (propertyName.IndexOf('.') > -1)
                {
                    // Requesting a child property
                    var propInfo = ReflectionHelper.GetProperty(EntityType, propertyName);
                    return propInfo.DeclaringType.GetEntityConfiguration()[propInfo.Name];
                }
                else
                {
                    return Properties[propertyName];
                }
            }
        }

        #endregion

        public class PropertySetChangeLoggingConfiguration
        {
            public PropertySetChangeLoggingConfiguration()
            {
                AuditedProperties = new List<string>();
            }

            public virtual string Namespace { get; internal set; }
            public virtual IList<string> AuditedProperties { get; internal set; }
        }
    }

}
