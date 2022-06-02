using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Entities;
using Abp.Runtime.Caching;
using Shesha.Configuration.Runtime;
using Shesha.Domain;
using Shesha.Domain.Attributes;
using Shesha.DynamicEntities;
using Shesha.DynamicEntities.Cache;
using Shesha.DynamicEntities.Dtos;
using Shesha.Extensions;
using Shesha.JsonLogic;
using Shesha.Metadata;
using Shesha.Metadata.Dtos;
using Shesha.Reflection;
using Shesha.Services;
using Shesha.Utilities;
using Shesha.Web.DataTable.Columns;

namespace Shesha.Web.DataTable
{
    /// inheritedDoc
    public class DataTableHelper : IDataTableHelper, ITransientDependency
    {
        private readonly IEntityConfigurationStore _entityConfigurationStore;
        private readonly IMetadataProvider _metadataProvider;
        private readonly IEntityConfigCache _entityConfigCache;
        private readonly IModelConfigurationProvider _modelConfigurationProvider;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="entityConfigurationStore"></param>
        /// <param name="metadataProvider"></param>
        /// <param name="entityConfigCache"></param>
        /// <param name="modelConfigurationProvider"></param>
        public DataTableHelper(IEntityConfigurationStore entityConfigurationStore, IMetadataProvider metadataProvider, IEntityConfigCache entityConfigCache, IModelConfigurationProvider modelConfigurationProvider)
        {
            _entityConfigurationStore = entityConfigurationStore;
            _metadataProvider = metadataProvider;
            _entityConfigCache = entityConfigCache;
            _modelConfigurationProvider = modelConfigurationProvider;
        }

        public void AppendQuickSearchCriteria(DataTableConfig tableConfig, QuickSearchMode searchMode, string sSearch, FilterCriteria filterCriteria)
        {
            AppendQuickSearchCriteria(tableConfig.RowType, tableConfig.Columns, searchMode, sSearch, filterCriteria, tableConfig.OnRequestToQuickSearch, tableConfig.Id);
        }

        /// inheritedDoc
        public void AppendQuickSearchCriteria(Type rowType, List<DataTableColumn> columns, QuickSearchMode searchMode, string sSearch, FilterCriteria filterCriteria, Action<FilterCriteria, string> onRequestToQuickSearch, string cacheKey)
        {
            if (string.IsNullOrWhiteSpace(sSearch))
                return;
            if (!rowType.IsEntityType())
                return;

            var subQueries = new List<string>();

            if (searchMode == QuickSearchMode.Sql || searchMode == QuickSearchMode.Combined)
            {
                // get list of properties existing in the table configuration
                var props = GetPropertiesForSqlQuickSearch(rowType, columns, cacheKey);

                if (!props.Any())
                    subQueries.Add("0=1");

                var addSubQuery = new Action<string, object>((q, v) =>
                {
                    var queryParamName = "p" + filterCriteria.FilterParameters.Count.ToString();
                    var criteria = string.Format((string)q, ":" + queryParamName);
                    subQueries.Add(criteria);

                    filterCriteria.FilterParameters.Add(queryParamName, v);
                });

                foreach (var prop in props)
                {
                    switch (prop.Value)
                    {
                        case GeneralDataType.Text:
                            {
                                if (!prop.Key.Contains('.'))
                                {
                                    addSubQuery($"ent.{prop.Key} like {{0}}", "%" + sSearch + "%");
                                }
                                else
                                {
                                    // use `exists` for nested entities because NH uses inner joins
                                    var nestedEntity = prop.Key.LeftPart('.', ProcessDirection.RightToLeft);
                                    var nestedProp = prop.Key.RightPart('.', ProcessDirection.RightToLeft);

                                    addSubQuery($@"exists (from ent.{nestedEntity} where {nestedProp} like {{0}})", "%" + sSearch + "%");
                                }
                                break;
                            }
                    }
                }
            }

            // add custom quick search logic
            if (onRequestToQuickSearch != null)
            {
                var quickSearchCriteria = new FilterCriteria(FilterCriteria.FilterMethod.Hql);

                // copy parameters to fix numbering todo: review and make parameters unique
                foreach (var paramName in filterCriteria.FilterParameters.Keys)
                {
                    quickSearchCriteria.FilterParameters.Add(paramName, filterCriteria.FilterParameters[paramName]);
                }

                onRequestToQuickSearch.Invoke(quickSearchCriteria, sSearch);
                if (quickSearchCriteria.FilterClauses.Any())
                {
                    subQueries.AddRange(quickSearchCriteria.FilterClauses);
                    var missingParameters = quickSearchCriteria.FilterParameters.Keys
                        .Where(p => !filterCriteria.FilterParameters.Keys.Contains(p)).ToList();
                    foreach (var missingParameter in missingParameters)
                    {
                        filterCriteria.FilterParameters.Add(missingParameter, quickSearchCriteria.FilterParameters[missingParameter]);
                    }
                }
            }

            if (subQueries.Any())
                filterCriteria.FilterClauses.Add(subQueries.Delimited(" or "));
        }

        /// <summary>
        /// Returns a list of properties for the SQL quick search
        /// </summary>
        public List<KeyValuePair<string, GeneralDataType>> GetPropertiesForSqlQuickSearch(Type rowType, List<DataTableColumn> columns, string cacheKey)
        {
            if (string.IsNullOrWhiteSpace(cacheKey))
                return DoGetPropertiesForSqlQuickSearch(rowType, columns);

            var cacheManager = StaticContext.IocManager.Resolve<ICacheManager>();

            return cacheManager
                .GetCache<string, List<KeyValuePair<string, GeneralDataType>>>("MyCache")
                .Get(cacheKey, (s) => DoGetPropertiesForSqlQuickSearch(rowType, columns));
        }

        private List<KeyValuePair<string, GeneralDataType>> DoGetPropertiesForSqlQuickSearch(Type rowType, List<DataTableColumn> columns)
        {
            var entityConfig = _entityConfigurationStore.Get(rowType);

            var props = columns
                .OfType<DataTablesDisplayPropertyColumn>()
                .Select(c =>
                {
                    var currentEntityConfig = entityConfig;
                    PropertyConfiguration property = null;
                    if (c.PropertyName.Contains('.'))
                    {
                        var parts = c.PropertyName.Split('.');

                        for (int i = 0; i < parts.Length; i++)
                        {
                            if (!currentEntityConfig.Properties.TryGetValue(parts[i], out property))
                                return null;

                            if (!property.IsMapped)
                                return null;

                            // all parts except the latest - entity reference
                            // all parts mapped

                            if (property.GeneralType == GeneralDataType.EntityReference)
                            {
                                currentEntityConfig = _entityConfigurationStore.Get(property.PropertyInfo.PropertyType);
                            }
                            else
                                if (i != parts.Length - 1)
                                return null; // only last part can be not an entity reference
                        }
                    }
                    else
                        currentEntityConfig.Properties.TryGetValue(c.PropertyName, out property);

                    if (property == null)
                        return null;

                    if (property.PropertyInfo.Name == currentEntityConfig.CreatedUserPropertyInfo?.Name ||
                        property.PropertyInfo.Name == currentEntityConfig.LastUpdatedUserPropertyInfo?.Name ||
                        property.PropertyInfo.Name == currentEntityConfig.InactivateUserPropertyInfo?.Name)
                        return null;

                    if (!property.IsMapped)
                        return null;

                    return new
                    {
                        Path = c.PropertyName,
                        Property = property
                    };
                })
                .Where(i => i != null)
                .Select(i => new KeyValuePair<string, GeneralDataType>(i.Path, i.Property.GeneralType))
                .ToList();

            return props;
        }

        /// <summary>
        /// Converts <see cref="Shesha.Configuration.Runtime.GeneralDataType"/> to data type for datatable column
        /// </summary>
        /// <param name="generalType"></param>
        /// <returns></returns>
        public static string GeneralDataType2ColumnDataType(GeneralDataType generalType)
        {
            switch (generalType)
            {
                case GeneralDataType.Boolean:
                    return ColumnDataTypes.Boolean;

                case GeneralDataType.ReferenceList:
                    return ColumnDataTypes.ReferenceList;

                case GeneralDataType.MultiValueReferenceList:
                    return ColumnDataTypes.MultiValueReferenceList;

                case GeneralDataType.EntityReference:
                    return ColumnDataTypes.EntityReference;

                case GeneralDataType.Date:
                    return ColumnDataTypes.Date;
                case GeneralDataType.DateTime:
                    return ColumnDataTypes.DateTime;

                case GeneralDataType.Time:
                    return ColumnDataTypes.Time;
                case GeneralDataType.Numeric:
                    return ColumnDataTypes.Number;
                case GeneralDataType.Text:
                    return ColumnDataTypes.String;

                default:
                    return ColumnDataTypes.String;
            }
        }

        public static string DataType2ColumnDataType(string dataType, string dataFormat)
        {
            switch (dataType)
            {
                case DataTypes.Boolean:
                    return ColumnDataTypes.Boolean;

                case DataTypes.ReferenceListItem:
                    return ColumnDataTypes.ReferenceList;

                    /*
                case DataTypes.Array:
                    return ColumnDataTypes.MultiValueReferenceList;
                    */
                case DataTypes.EntityReference:
                    return ColumnDataTypes.EntityReference;

                case DataTypes.Date:
                    return ColumnDataTypes.Date;
                case DataTypes.DateTime:
                    return ColumnDataTypes.DateTime;

                case DataTypes.Time:
                    return ColumnDataTypes.Time;
                case DataTypes.Number:
                    return ColumnDataTypes.Number;
                case DataTypes.String:
                    return ColumnDataTypes.String;

                default:
                    return ColumnDataTypes.String;
            }
        }

        /// <summary>
        /// Fill metadata of the <see cref="JsonLogic2HqlConverterContext"/> with specified columns
        /// </summary>
        public static void FillContextMetadata(List<DataTableColumn> columns, JsonLogic2HqlConverterContext context)
        {
            context.FieldsMetadata = columns.ToDictionary(
                c => c.Name,
                c => {
                    return new PropertyMetadata
                    {
                        Name = c.Name,
                        Label = c.Caption,
                        Description = c.Description,
                        DataType = c.StandardDataType,
                    } as IPropertyMetadata;
                }
            );
        }

        /// <summary>
        /// Fill metadata of the <see cref="JsonLogic2HqlConverterContext"/> with columns of the specified <paramref name="tableConfig"/>
        /// </summary>
        public static void FillContextMetadata(List<PropertyMetadataDto> properties, JsonLogic2HqlConverterContext context)
        {
            context.FieldsMetadata = properties.ToDictionary(
                c => c.Path,
                c => new PropertyMetadata
                {
                    Name = c.Path,
                    Label = c.Label,
                    Description = c.Description,
                    DataType = c.DataType
                } as IPropertyMetadata
            );
        }

        /// <summary>
        /// Fill variable resolvers of the <see cref="JsonLogic2HqlConverterContext"/> with columns of the specified <paramref name="tableConfig"/>
        /// </summary>
        public static void FillVariablesResolvers(List<DataTableColumn> columns, JsonLogic2HqlConverterContext context)
        {
            context.VariablesResolvers = columns.ToDictionary(c => c.Name, c => c.PropertyName);
        }

        /// <summary>
        /// Fill variable resolvers of the <see cref="JsonLogic2HqlConverterContext"/> with columns of the specified <paramref name="tableConfig"/>
        /// </summary>
        public static void FillVariablesResolvers(List<PropertyMetadataDto> properties, JsonLogic2HqlConverterContext context)
        {
            context.VariablesResolvers = properties.ToDictionary(c => c.Path, c => c.Path);
        }

        /// inheritedDoc
        [Obsolete]
        public DataTablesDisplayPropertyColumn GetDisplayPropertyColumn(Type rowType, string propName, string name = null)
        {
            var prop = propName == null
                ? null
                : ReflectionHelper.GetProperty(rowType, propName);

            var displayAttribute = prop != null
                ? prop.GetAttribute<DisplayAttribute>()
                : null;

            var caption = displayAttribute != null && !string.IsNullOrWhiteSpace(displayAttribute.Name)
                ? displayAttribute.Name
                : propName.ToFriendlyName();

            var dataTypeInfo = prop != null
                ? _metadataProvider.GetDataType(prop)
                : new DataTypeInfo(null);
            var column = new DataTablesDisplayPropertyColumn()
            {
                Name = (propName ?? "").Replace('.', '_'),
                PropertyName = propName,
                Caption = caption,
                Description = prop?.GetDescription(),
                StandardDataType = dataTypeInfo.DataType,
                DataFormat = dataTypeInfo.DataFormat,

                #region backward compatibility, to be removed
                GeneralDataType = prop != null
                    ? EntityConfigurationLoaderByReflection.GetGeneralDataType(prop)
                    : (GeneralDataType?)null,
                CustomDataType = prop?.GetAttribute<DataTypeAttribute>()?.CustomDataType,
                #endregion
            };
            var entityConfig = prop?.DeclaringType.GetEntityConfiguration();
            var propConfig = prop != null ? entityConfig?.Properties[prop.Name] : null;
            if (propConfig != null)
            {
                column.ReferenceListName = propConfig.ReferenceListName;
                column.ReferenceListNamespace = propConfig.ReferenceListNamespace;
                if (propConfig.EntityReferenceType != null)
                {
                    column.EntityReferenceTypeShortAlias = propConfig.EntityReferenceType.GetEntityConfiguration()?.SafeTypeShortAlias;
                    column.AllowInherited = propConfig.PropertyInfo.HasAttribute<AllowInheritedAttribute>();
                }
            }

            if (prop == null)
            {
                var modelConfig = AsyncHelper.RunSync<ModelConfigurationDto>(() => _modelConfigurationProvider.GetModelConfigurationOrNullAsync(rowType.Namespace, rowType.Name));
                var propertyConfig = modelConfig.Properties.FirstOrDefault(p => p.Name == propName);
                if (propertyConfig != null) 
                {
                    column.IsDynamic = true;
                    column.StandardDataType = propertyConfig.DataType;
                    column.DataFormat = propertyConfig.DataFormat;
                    column.Description = propertyConfig.Description;
                    column.IsFilterable = false;
                    column.IsSortable = false;
                }
            }

            // Set FilterCaption and FilterPropertyName
            column.FilterCaption ??= column.Caption;
            column.FilterPropertyName ??= column.PropertyName;

            if (column.PropertyName == null)
            {
                column.PropertyName = column.FilterPropertyName;
                column.Name = (column.PropertyName ?? "").Replace('.', '_');
            }
            column.Caption ??= column.FilterCaption;

            // Check is the property mapped to the DB. If it's not mapped - make the column non sortable and non filterable
            if (column.IsSortable && rowType.IsEntityType() && propName != null && propName != "Id")
            {
                var chain = propName.Split('.').ToList();

                var container = rowType;
                foreach (var chainPropName in chain)
                {
                    if (!container.IsEntityType())
                        break;

                    var containerConfig = container.GetEntityConfiguration();
                    var propertyConfig = containerConfig.Properties.ContainsKey(chainPropName)
                        ? containerConfig.Properties[chainPropName]
                        : null;

                    if (propertyConfig != null && !propertyConfig.IsMapped)
                    {
                        column.IsFilterable = false;
                        column.IsSortable = false;
                        break;
                    }

                    container = propertyConfig?.PropertyInfo.PropertyType;
                    if (container == null)
                        break;
                }
            }

            return column;
        }

        /// inheritedDoc
        public async Task<DataTablesDisplayPropertyColumn> GetDisplayPropertyColumnAsync(Type rowType, string propName, string name = null) 
        {
            var prop = propName == null
                ? null
                : ReflectionHelper.GetProperty(rowType, propName);

            var displayAttribute = prop != null
                ? prop.GetAttribute<DisplayAttribute>()
                : null;

            var caption = displayAttribute != null && !string.IsNullOrWhiteSpace(displayAttribute.Name)
                ? displayAttribute.Name
                : propName.ToFriendlyName();

            var dataTypeInfo = prop != null
                ? _metadataProvider.GetDataType(prop)
                : new DataTypeInfo(null);
            var column = new DataTablesDisplayPropertyColumn()
            {
                Name = (propName ?? "").Replace('.', '_'),
                PropertyName = propName,
                Caption = caption,
                Description = prop?.GetDescription(),
                StandardDataType = dataTypeInfo.DataType,
                DataFormat = dataTypeInfo.DataFormat,

                #region backward compatibility, to be removed
                GeneralDataType = prop != null
                    ? EntityConfigurationLoaderByReflection.GetGeneralDataType(prop)
                    : (GeneralDataType?)null,
                CustomDataType = prop?.GetAttribute<DataTypeAttribute>()?.CustomDataType,
                #endregion
            };
            var entityConfig = prop?.DeclaringType.GetEntityConfiguration();
            var propConfig = prop != null ? entityConfig?.Properties[prop.Name] : null;
            if (propConfig != null)
            {
                column.ReferenceListName = propConfig.ReferenceListName;
                column.ReferenceListNamespace = propConfig.ReferenceListNamespace;
                if (propConfig.EntityReferenceType != null)
                {
                    column.EntityReferenceTypeShortAlias = propConfig.EntityReferenceType.GetEntityConfiguration()?.SafeTypeShortAlias;
                    column.AllowInherited = propConfig.PropertyInfo.HasAttribute<AllowInheritedAttribute>();
                }
            }

            if (prop == null)
            {
                var modelConfig = await _modelConfigurationProvider.GetModelConfigurationOrNullAsync(rowType.Namespace, rowType.Name);
                var propertyConfig = modelConfig.Properties.FirstOrDefault(p => p.Name == propName);
                if (propertyConfig != null)
                {
                    column.IsDynamic = true;
                    column.StandardDataType = propertyConfig.DataType;
                    column.DataFormat = propertyConfig.DataFormat;
                    column.Description = propertyConfig.Description;
                    column.IsFilterable = false;
                    column.IsSortable = false;
                }
            }

            // Set FilterCaption and FilterPropertyName
            column.FilterCaption ??= column.Caption;
            column.FilterPropertyName ??= column.PropertyName;

            if (column.PropertyName == null)
            {
                column.PropertyName = column.FilterPropertyName;
                column.Name = (column.PropertyName ?? "").Replace('.', '_');
            }
            column.Caption ??= column.FilterCaption;

            // Check is the property mapped to the DB. If it's not mapped - make the column non sortable and non filterable
            if (column.IsSortable && rowType.IsEntityType() && propName != null && propName != "Id")
            {
                var chain = propName.Split('.').ToList();

                var container = rowType;
                foreach (var chainPropName in chain)
                {
                    if (!container.IsEntityType())
                        break;

                    var containerConfig = container.GetEntityConfiguration();
                    var propertyConfig = containerConfig.Properties.ContainsKey(chainPropName)
                        ? containerConfig.Properties[chainPropName]
                        : null;

                    if (propertyConfig != null && !propertyConfig.IsMapped)
                    {
                        column.IsFilterable = false;
                        column.IsSortable = false;
                        break;
                    }

                    container = propertyConfig?.PropertyInfo.PropertyType;
                    if (container == null)
                        break;
                }
            }

            return column;
        }
    }
}
