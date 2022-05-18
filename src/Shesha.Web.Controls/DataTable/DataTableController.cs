using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Dependency;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.ObjectMapping;
using Abp.Runtime.Session;
using Abp.Runtime.Validation;
using Castle.Core.Logging;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using NHibernate;
using Shesha.Configuration.Runtime;
using Shesha.Domain;
using Shesha.Domain.Enums;
using Shesha.Domain.QueryBuilder;
using Shesha.Extensions;
using Shesha.JsonLogic;
using Shesha.Metadata;
using Shesha.NHibernate.Session;
using Shesha.Reflection;
using Shesha.Services;
using Shesha.Utilities;
using Shesha.Web.DataTable.Columns;
using Shesha.Web.DataTable.Excel;
using Shesha.Web.DataTable.Model;
using Shesha.Web.DataTable.QueryBuilder;

namespace Shesha.Web.DataTable
{
    /// <summary>
    /// Controller of the DataTable control
    /// </summary>
    [AbpAuthorize()]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class DataTableController: ControllerBase, ITransientDependency
    {
        private readonly IDataTableConfigurationStore _configurationStore;
        private readonly IObjectMapper _objectMapper;
        private readonly IIocResolver _iocResolver;
        private readonly IRepository<StoredFilter, Guid> _filterRepository;
        private readonly IRepository<ShaRole, Guid> _roleRepository;
        private readonly IRepository<ShaRoleAppointedPerson, Guid> _rolePersonRepository;
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly IJsonLogic2HqlConverter _jsonLogic2HqlConverter;
        private readonly IDataTableHelper _helper;
        private readonly IEntityConfigurationStore _entityConfigStore;
        private readonly IMetadataProvider _metadataProvider;

        public ILogger Logger { get; set; } = new NullLogger();

        /// <summary>
        /// 
        /// </summary>
        public DataTableController(IDataTableConfigurationStore configurationStore, IObjectMapper objectMapper, IIocResolver iocResolver, IRepository<StoredFilter, Guid> filterRepository, IRepository<ShaRole, Guid> roleRepository, IRepository<ShaRoleAppointedPerson, Guid> rolePersonRepository, IUnitOfWorkManager unitOfWorkManager, IJsonLogic2HqlConverter jsonLogic2HqlConverter, IDataTableHelper helper, IEntityConfigurationStore entityConfigStore, IMetadataProvider metadataProvider)
        {
            _configurationStore = configurationStore;
            _objectMapper = objectMapper;
            _iocResolver = iocResolver;
            _filterRepository = filterRepository;
            _roleRepository = roleRepository;
            _rolePersonRepository = rolePersonRepository;
            _unitOfWorkManager = unitOfWorkManager;
            _jsonLogic2HqlConverter = jsonLogic2HqlConverter;
            _helper = helper;
            _entityConfigStore = entityConfigStore;
            _metadataProvider = metadataProvider;
        }

        /// <summary>
        /// Returns configuration of the DataTable by <paramref name="id"/>
        /// </summary>
        [HttpGet]
        public DataTableConfigDto GetConfiguration(string id, CancellationToken cancellationToken)
        {
            // todo: check existence of the config
            var config = _configurationStore.GetTableConfiguration(id, false);
            if (config == null)
                return null;

            config.Columns = config.Columns.Where(c => c.IsAuthorized == null || c.IsAuthorized.Invoke()).ToList();
            var dto = _objectMapper.Map<DataTableConfigDto>(config);

            return dto;
        }

        /// <summary>
        /// Returns datatable columns for configurable table. Accepts type of model(entity) and list of properties.
        /// Columns configuration is merged on the client side with configurable values
        /// </summary>
        [HttpPost]
        public List<DataTableColumnDto> GetColumns(GetColumnsInput input, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(input.EntityType))
                throw new AbpValidationException($"'{nameof(input.EntityType)}' must not be null");

            var entityType = !string.IsNullOrWhiteSpace(input.EntityType)
                ? _entityConfigStore.Get(input.EntityType)
                : null;
            if (entityType == null)
                throw new AbpValidationException($"Entity of type `{input.EntityType}` not found");

            var columns = GetColumnsForProperties(entityType.EntityType, input.Properties);
            // DataTableColumnDto
            var dtos = columns.Select(c => _objectMapper.Map<DataTableColumnDto>(c)).ToList();

            return dtos;
        }

        private List<DataTableColumn> GetColumnsForProperties(Type rowType, List<string> properties) 
        {
            var columns = properties.Select(p => _helper.GetDisplayPropertyColumn(rowType, p, p))
                .Cast<DataTableColumn>()
                .ToList();

            return columns;
        }


        /// <summary>
        /// Returns data for the DateTable control
        /// </summary>
        [HttpPost]
        public async Task<DataTableData> GetData(DataTableGetDataInput input, CancellationToken cancellationToken)
        {

            if (!string.IsNullOrWhiteSpace(input.Id))
            {
                // support of table configurations, may be removed later
                var tableConfig = !string.IsNullOrWhiteSpace(input.Id)
                    ? _configurationStore.GetTableConfiguration(input.Id)
                    : null;
                if (!string.IsNullOrWhiteSpace(input.Id) && tableConfig == null)
                    throw new AbpValidationException($"Table configuration with Id = '{input.Id}' not found");

                return await GetDataInternal(tableConfig.RowType, tableConfig.IdType, input, cancellationToken);
            }
            else
            if (!string.IsNullOrWhiteSpace(input.EntityType)) 
            {
                // support of configurable tables (forms designer)
                var entityConfig = _entityConfigStore.Get(input.EntityType);
                if (entityConfig == null)
                    throw new AbpValidationException($"Entity of type '{input.EntityType}' not found");

                return await GetDataInternal(entityConfig.EntityType, entityConfig.Properties[SheshaDatabaseConsts.IdColumn].PropertyInfo.PropertyType, input, cancellationToken);
            }
            else
                throw new AbpValidationException($"'{nameof(input.Id)}' or '{nameof(input.EntityType)}' must be specified");
        }

        private async Task<DataTableData> GetDataInternal(Type rowType, Type idType, DataTableGetDataInput input, CancellationToken cancellationToken)
        {
            var method = this.GetType().GetMethod(nameof(GetTableDataAsync));
            if (method == null)
                throw new Exception($"{nameof(GetTableDataAsync)} not found");

            var genericMethod = method.MakeGenericMethod(rowType, idType);

            var task = (Task)genericMethod.Invoke(this, new object[] { input, cancellationToken });
            await task.ConfigureAwait(false);

            var resultProperty = task.GetType().GetProperty("Result");
            if (resultProperty == null)
                throw new Exception("Result property not found");

            return resultProperty.GetValue(task) as DataTableData;
        }

        /// <summary>
        /// Exports DataTable to Excel
        /// </summary>
        [HttpPost]
        public async Task<FileStreamResult> ExportToExcel(DataTableGetDataInput input, CancellationToken cancellationToken)
        {
            input.CurrentPage = 1;
            input.PageSize = int.MaxValue;

            var method = this.GetType().GetMethod(nameof(GetTableQueryDataAsync));
            if (method == null)
                throw new Exception($"{nameof(GetTableQueryDataAsync)} not found");

            if (!string.IsNullOrWhiteSpace(input.Id))
            {
                // support of table configurations, may be removed later
                var tableConfig = !string.IsNullOrWhiteSpace(input.Id)
                    ? _configurationStore.GetTableConfiguration(input.Id)
                    : null;
                if (!string.IsNullOrWhiteSpace(input.Id) && tableConfig == null)
                    throw new AbpValidationException($"Table configuration with Id = '{input.Id}' not found");

                var genericMethod = method.MakeGenericMethod(tableConfig.RowType, tableConfig.IdType);

                var task = (Task)genericMethod.Invoke(this, new object[] { input, cancellationToken });
                await task.ConfigureAwait(false);

                var resultProperty = task.GetType().GetProperty("Result");
                if (resultProperty == null)
                    throw new Exception("Result property not found");

                var data = resultProperty.GetValue(task) as IQueryDataDto;
                if (data == null)
                    throw new Exception("Failed to export to Excel");

                return GetExcelResult(data.Rows, tableConfig.Columns);
            }
            else
            if (!string.IsNullOrWhiteSpace(input.EntityType))
            {
                // support of configurable tables (forms designer)
                var entityConfig = _entityConfigStore.Get(input.EntityType);
                if (entityConfig == null)
                    throw new AbpValidationException($"Entity of type '{input.EntityType}' not found");

                var idType = entityConfig.Properties[SheshaDatabaseConsts.IdColumn].PropertyInfo.PropertyType;
                var genericMethod = method.MakeGenericMethod(entityConfig.EntityType, idType);

                var task = (Task)genericMethod.Invoke(this, new object[] { input, cancellationToken });
                await task.ConfigureAwait(false);

                var resultProperty = task.GetType().GetProperty("Result");
                if (resultProperty == null)
                    throw new Exception("Result property not found");

                var data = resultProperty.GetValue(task) as IQueryDataDto;
                if (data == null)
                    throw new Exception("Failed to export to Excel");

                var columns = GetColumnsFromInput(input);
                return GetExcelResult(data.Rows, columns);
            }
            else
                throw new AbpValidationException($"'{nameof(input.Id)}' or '{nameof(input.EntityType)}' must be specified");
        }

        private FileStreamResult GetExcelResult(IList rows, IList<DataTableColumn> columns) 
        {
            var excelFileName = "Export.xlsx";
            HttpContext.Response.Headers.Add("content-disposition", $"attachment;filename={excelFileName}");

            var stream = ExcelUtility.ReadToExcelStream(rows, columns);

            stream.Seek(0, SeekOrigin.Begin);

            return new FileStreamResult(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }

        private IDisposable HandleSoftDeletedFilter<TEntity>(DataTableGetDataInput input)
        {
            if (typeof(ISoftDelete).IsAssignableFrom(typeof(TEntity)))
            {
                var isDeletedFilter = input.Filter.FirstOrDefault(f => f.RealPropertyName == nameof(ISoftDelete.IsDeleted));
                if (isDeletedFilter != null)
                {
                    if (GetBoolean(isDeletedFilter.Filter) == true)
                        return _unitOfWorkManager.Current.DisableFilter(AbpDataFilters.SoftDelete);
                }
            }

            return null;
        }

        /// <summary>
        /// Generic version of GetTableDataAsync. Note: marked public for reflection
        /// </summary>
        [UsedImplicitly]
        public async Task<DataTableData> GetTableDataAsync<TEntity, TPrimaryKey>(DataTableGetDataInput input, CancellationToken cancellationToken) where TEntity : class, IEntity<TPrimaryKey>
        {
            var queryData = await GetTableQueryDataAsync<TEntity, TPrimaryKey>(input, cancellationToken);

            var columns = GetColumnsFromInput(input);
            return GetTableDataWithPaging(queryData, columns, input.PageSize, cancellationToken);
        }

        public async Task<QueryDataDto<TEntity, TPrimaryKey>> GetTableQueryDataAsync<TEntity, TPrimaryKey>(DataTableGetDataInput input, CancellationToken cancellationToken) where TEntity : class, IEntity<TPrimaryKey>
        {
            var tableConfig = !string.IsNullOrWhiteSpace(input.Id)
                ? _configurationStore.GetTableConfiguration(input.Id)
                : null;

            cancellationToken.ThrowIfCancellationRequested();

            using (HandleSoftDeletedFilter<TEntity>(input))
            {
                QueryDataDto<TEntity, TPrimaryKey> queryData = null;
                if (tableConfig != null)
                {
                    queryData = await QueryRepositoryAsync<TEntity, TPrimaryKey>(tableConfig, input, cancellationToken);
                    return queryData;
                }
                else
                {
                    var columns = GetColumnsFromInput(input);
                    queryData = await QueryRepositoryAsync<TEntity, TPrimaryKey>(columns, input, cancellationToken);
                    return queryData;
                }
            }
        }

        private List<DataTableColumn> GetColumnsFromInput(DataTableGetDataInput input) 
        {
            var tableConfig = !string.IsNullOrWhiteSpace(input.Id)
                ? _configurationStore.GetTableConfiguration(input.Id)
                : null;
            if (tableConfig != null)
            {
                var authorizedColumns = tableConfig.Columns.Where(c => c.IsAuthorized == null || c.IsAuthorized.Invoke()).ToList();
                return authorizedColumns;
            }
            else
            {
                var entityConfig = _entityConfigStore.Get(input.EntityType);
                if (entityConfig == null)
                    throw new AbpValidationException($"Entity of type '{input.EntityType}' not found");

                if (input.Properties == null)
                    throw new Exception("Properties not specified");

                var properties = input.Properties.ToList();
                if (!properties.Contains(SheshaDatabaseConsts.IdColumn))
                    properties.Insert(0, SheshaDatabaseConsts.IdColumn);

                var columns = properties.Select(p => _helper.GetDisplayPropertyColumn(entityConfig.EntityType, p))
                    .Cast<DataTableColumn>()
                    .ToList();
                return columns;
            }
        }

        private DataTableData GetTableDataWithPaging<TEntity, TPrimaryKey>(QueryDataDto<TEntity, TPrimaryKey> queryData, List<DataTableColumn> columns, int pageSize, CancellationToken cancellationToken) where TEntity : class, IEntity<TPrimaryKey>
        {
            var dataRows = MapDataRows(queryData.Rows, columns, cancellationToken);

            var totalPages = (int)Math.Ceiling((double)queryData.TotalRows / pageSize);

            var result = new DataTableData
            {
                TotalRowsBeforeFilter = queryData.TotalRowsBeforeFilter,
                TotalRows = queryData.TotalRows,
                TotalPages = totalPages,
                Rows = dataRows
            };
            
            return result;
        }            

        private List<Dictionary<string, object>> MapDataRows(IList queryDataRows, List<DataTableColumn> columns, CancellationToken cancellationToken) 
        {
            var dataRows = new List<Dictionary<string, object>>();
            foreach (var item in queryDataRows)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var row = new Dictionary<string, object>();

                foreach (var col in columns)
                {
                    try
                    {
                        var value = item != null
                            ? col.CellContent(item, false)
                            : null;

                        value ??= string.Empty;

                        row.Add(col.Name, value);
                    }
                    catch
                    {
                        throw;
                    }
                }

                dataRows.Add(row);
            }
            return dataRows;
        }

        /// <summary>
        /// Returns a list of data
        /// </summary>
        /// <param name="tableConfig">Configuration of the table</param>
        /// <param name="input">DataTable params (part of the request from the client-side)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns></returns>
        public async Task<QueryDataDto<TEntity, TPrimaryKey>> QueryRepositoryAsync<TEntity, TPrimaryKey>([NotNull]DataTableConfig tableConfig,
            [NotNull]DataTableGetDataInput input, CancellationToken cancellationToken) where TEntity : class, IEntity<TPrimaryKey>
        {
            try
            {
                var context = new DataTableQueryBuildingContext(tableConfig.RowType, tableConfig.Columns, input);

                AppendStandardFilterCriteria(context);

                // todo: Handle additional posted data (for child tables)
                AppendChildEntityFilterParameters(tableConfig, context);

                AppendStoredFilters(tableConfig, context);
                AppendPredefinedFilters(context);

                tableConfig.OnRequestToFilterStatic?.Invoke(context.FilterCriteria, input);
                if (tableConfig.OnRequestToFilterStaticAsync != null)
                    await tableConfig.OnRequestToFilterStaticAsync.Invoke(context.FilterCriteria, input);

                var data = new QueryDataDto<TEntity, TPrimaryKey>
                {
                    TotalRowsBeforeFilter = await CountAsync<TEntity, TPrimaryKey>(context, cancellationToken)
                };

                var filterBefore = context.FilterCriteria.Clone() as FilterCriteria;

                // Applying any Table Configuration specific filter logic.
                tableConfig.OnRequestToFilter?.Invoke(context.FilterCriteria, input);
                if (tableConfig.OnRequestToFilterAsync != null)
                    await tableConfig.OnRequestToFilterAsync.Invoke(context.FilterCriteria, input);

                var orderBy = GetOrderByClause(tableConfig.Columns, input, tableConfig.UserSortingDisabled);

                var takeCount = input.PageSize > -1 
                    ? input.PageSize 
                    : int.MaxValue;

                if (!string.IsNullOrWhiteSpace(input.QuickSearch))
                    _helper.AppendQuickSearchCriteria(tableConfig, tableConfig.QuickSearchMode, input.QuickSearch, context.FilterCriteria);

                var skipCount = Math.Max(0, (input.CurrentPage - 1) * takeCount);

                data.Entities = await FindAllAsync<TEntity, TPrimaryKey>(context.FilterCriteria, skipCount, takeCount, orderBy, cancellationToken);

                data.TotalRows = filterBefore != null && filterBefore.FilterClauses.Count == context.FilterCriteria.FilterClauses.Count
                    ? data.TotalRowsBeforeFilter
                    : await CountAsync<TEntity, TPrimaryKey>(context, cancellationToken);

                return data;
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Returns a list of data
        /// </summary>
        /// <param name="columns">Table columns</param>
        /// <param name="input">DataTable params (part of the request from the client-side)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns></returns>
        public async Task<QueryDataDto<TEntity, TPrimaryKey>> QueryRepositoryAsync<TEntity, TPrimaryKey>([NotNull] List<DataTableColumn> columns, [NotNull] DataTableGetDataInput input, CancellationToken cancellationToken) where TEntity : class, IEntity<TPrimaryKey>
        {
            try
            {
                var queryContext = new DataTableQueryBuildingContext(typeof(TEntity), columns, input);

                AppendStandardFilterCriteria(queryContext);

                // todo: implement support of child tables with configured columns
                //AppendChildEntityFilterParameters(tableConfig, input, filterCriteria);

                // todo: add support of static filters as it's done for datatable configs
                AppendPredefinedFilters(queryContext);

                // Append `order by`. Note: joins can be added during calculation of `order by` part
                AppendOrderBy(queryContext, false);

                var data = new QueryDataDto<TEntity, TPrimaryKey>
                {
                    TotalRowsBeforeFilter = await CountAsync<TEntity, TPrimaryKey>(queryContext, cancellationToken)
                };

                var filterBefore = queryContext.FilterCriteria.Clone() as FilterCriteria;

                var takeCount = input.PageSize > -1
                    ? input.PageSize
                    : int.MaxValue;

                if (!string.IsNullOrWhiteSpace(input.QuickSearch))
                    _helper.AppendQuickSearchCriteria(typeof(TEntity), columns, QuickSearchMode.Sql, input.QuickSearch, queryContext.FilterCriteria, null, !string.IsNullOrWhiteSpace(input.Id) ? input.Id : input.Uid);

                var skipCount = Math.Max(0, (input.CurrentPage - 1) * takeCount);

                data.Entities = await FindAllAsync<TEntity, TPrimaryKey>(queryContext, skipCount, takeCount, queryContext.OrderBy, cancellationToken);

                data.TotalRows = filterBefore != null && filterBefore.FilterClauses.Count == queryContext.FilterCriteria.FilterClauses.Count
                    ? data.TotalRowsBeforeFilter
                    : await CountAsync<TEntity, TPrimaryKey>(queryContext, cancellationToken);

                return data;
            }
            catch
            {
                throw;
            }
        }
        private void AppendChildEntityFilterParameters(DataTableConfig tableConfig, DataTableQueryBuildingContext queryContext)
        {
            if (!(tableConfig is IChildDataTableConfig childConfig))
                return;

            var filterCriteria = queryContext.FilterCriteria;

            if (string.IsNullOrWhiteSpace(queryContext.DataTableInput.ParentEntityId))
            {
                // if config is for child dataTable and parent info not passed - filter out all records
                filterCriteria.FilterClauses.Add("1=0");
                return;
            }

            var parsedId = Parser.ParseId(queryContext.DataTableInput.ParentEntityId, childConfig.ParentType);

            switch (childConfig.RelationshipType)
            {
                case RelationshipType.ManyToMany:
                    filterCriteria.FilterClauses.Add("exists (from " + childConfig.ParentType.FullName + " as par where par.Id = :parentId and ent in elements(par." + childConfig.Relationship_ChildsCollection + "))");
                    filterCriteria.FilterParameters.Add("parentId", parsedId);
                    break;
                case RelationshipType.OneToMany:
                    var paramName = childConfig.Relationship_LinkToParent.Replace(".", "_");
                    filterCriteria.FilterClauses.Add("ent." + childConfig.Relationship_LinkToParent + ".Id = :" + paramName);
                    filterCriteria.FilterParameters.Add(paramName, parsedId);
                    break;
                case RelationshipType.MultipleOwners:
                    break;
                default:
                    throw new NotSupportedException(
                        $"RelationshipType '{childConfig.RelationshipType}' is not supported");
            }
        }
        
        private void AppendStandardFilterCriteria(DataTableQueryBuildingContext queryContext)
        {
            foreach (var filter in queryContext.DataTableInput.Filter)
            {
                if (filter.Filter == null)
                    continue;

                var column = queryContext.Columns.FirstOrDefault(c => c.Name == filter.RealPropertyName);
                if (column?.GeneralDataType == null)
                    continue;
                var filterCriteria = queryContext.FilterCriteria;

                // workaround for booleans (we support only `equals`)
                if (column.GeneralDataType == GeneralDataType.Boolean ||
                    column.GeneralDataType == GeneralDataType.EntityReference)
                    filter.FilterOption = FilterOperations.Equals;

                if (string.IsNullOrWhiteSpace(filter.FilterOption) && (column.GeneralDataType == GeneralDataType.ReferenceList || column.GeneralDataType == GeneralDataType.MultiValueReferenceList))
                    filter.FilterOption = FilterOperations.Contains;


                // todo: check column types and filter type
                switch (filter.FilterOption)
                {
                    case FilterOperations.Contains:
                    {
                        switch (column.GeneralDataType)
                        {
                            case GeneralDataType.ReferenceList:
                            case GeneralDataType.MultiValueReferenceList:
                            {
                                var array = GetArray(filter.Filter);
                                if (array != null && array.Any())
                                {
                                    var refListFilter = array.Select(i => GetDecimal(i))
                                        .Where(i => i.HasValue)
                                        .Select(i => $"ent.{column.PropertyName} = {i.Value}")
                                        .Delimited(" or ");

                                    if (!string.IsNullOrWhiteSpace(refListFilter))
                                        filterCriteria.FilterClauses.Add(refListFilter);

                                }
                                else
                                {
                                    // backward compatibility
                                    var decimalValue = GetDecimal(filter.Filter);
                                    if (decimalValue.HasValue)
                                        filterCriteria.AddParameterisedCriterion("ent." + column.PropertyName + " = {0}",
                                            decimalValue.Value);
                                }

                                break;
                            }
                            default:
                            {
                                // strings only
                                var strValue = GetString(filter.Filter);
                                if (!string.IsNullOrWhiteSpace(strValue))
                                    filterCriteria.AddParameterisedCriterion("ent." + column.PropertyName + " like {0}", "%" + strValue + "%");
                                break;
                            }
                        }
                        break;
                    }
                    case FilterOperations.StartsWith:
                    {
                        // strings only
                        var strValue = GetString(filter.Filter);
                        if (!string.IsNullOrWhiteSpace(strValue))
                            filterCriteria.AddParameterisedCriterion("ent." + column.PropertyName + " like {0}", strValue + "%");
                        break;
                    }
                    case FilterOperations.EndsWith:
                    {
                            // strings only
                        var strValue = GetString(filter.Filter);
                        if (!string.IsNullOrWhiteSpace(strValue))
                            filterCriteria.AddParameterisedCriterion("ent." + column.PropertyName + " like {0}", "%" + strValue);
                        break;
                    }
                    case FilterOperations.Equals:
                    {
                        switch (column.GeneralDataType)
                        {
                            case GeneralDataType.Text:
                                var strValue = GetString(filter.Filter);
                                if (!string.IsNullOrWhiteSpace(strValue))
                                    filterCriteria.AddParameterisedCriterion("ent." + column.PropertyName + " = {0}", strValue);
                                break;
                            case GeneralDataType.Numeric:
                            case GeneralDataType.ReferenceList:
                            case GeneralDataType.MultiValueReferenceList:
                            {
                                var decimalValue = GetDecimal(filter.Filter);
                                if (decimalValue.HasValue)
                                    filterCriteria.AddParameterisedCriterion("ent." + column.PropertyName + " = {0}",
                                        decimalValue.Value);
                                break;
                            }
                            case GeneralDataType.EntityReference:
                            {
                                var id = GetString(filter.Filter)?.ToGuid();
                                if (id != Guid.Empty)
                                    filterCriteria.AddParameterisedCriterion("ent." + column.PropertyName + ".Id = {0}", id.Value);
                                break;
                            }
                            case GeneralDataType.Date:
                            {
                                var dateValue = GetDate(filter.Filter);
                                if (dateValue.HasValue)
                                {
                                    filterCriteria.AddParameterisedCriterion($"ent.{column.PropertyName} >= {{0}}",
                                        dateValue.Value.Date.StartOfTheDay());
                                    filterCriteria.AddParameterisedCriterion($"ent.{column.PropertyName} <= {{0}}",
                                        dateValue.Value.EndOfTheDay());
                                }

                                break;
                            }
                            case GeneralDataType.DateTime:
                            {
                                var dateValue = GetDate(filter.Filter, out var withTime);
                                if (dateValue.HasValue)
                                {
                                    var dateFrom = withTime
                                        ? dateValue.Value.Date.StripSeconds()
                                        : dateValue.Value.StartOfTheDay();

                                    var dateTo = withTime
                                        ? dateValue.Value.Date.StripSeconds().AddMinutes(1)
                                        : dateValue.Value.EndOfTheDay();

                                    filterCriteria.AddParameterisedCriterion($"ent.{column.PropertyName} >= {{0}}", dateFrom);
                                    filterCriteria.AddParameterisedCriterion($"ent.{column.PropertyName} < {{0}}", dateTo);
                                }

                                break;
                            }
                            case GeneralDataType.Time:
                            {
                                var timeValue = GetTime(filter.Filter);
                                if (timeValue.HasValue)
                                {
                                    filterCriteria.AddParameterisedCriterion($"ent.{column.PropertyName} >= {{0}}",
                                        timeValue.Value.StripSeconds());
                                    filterCriteria.AddParameterisedCriterion($"ent.{column.PropertyName} < {{0}}",
                                        timeValue.Value.StripSeconds() + TimeSpan.FromMinutes(1));
                                }

                                break;
                            }
                            case GeneralDataType.Boolean:
                            {
                                var boolValue = GetBoolean(filter.Filter);
                                if (boolValue.HasValue)
                                    filterCriteria.AddParameterisedCriterion("ent." + column.PropertyName + " = {0}",
                                        boolValue.Value);
                                break;
                            }
                        }
                        break;
                    }
                    case FilterOperations.LessThan:
                    {
                        // numeric
                        var decimalValue = GetDecimal(filter.Filter);
                        if (decimalValue.HasValue)
                            filterCriteria.AddParameterisedCriterion("ent." + column.PropertyName + " < {0}", decimalValue.Value);
                        break;
                    }
                    case FilterOperations.GreaterThan:
                    {
                        // numeric
                        var decimalValue = GetDecimal(filter.Filter);
                        if (decimalValue.HasValue)
                            filterCriteria.AddParameterisedCriterion("ent." + column.PropertyName + " > {0}", decimalValue.Value);
                        break;
                    }
                    case FilterOperations.Between:
                    {
                        // numeric, date
                        var range = GetArray(filter.Filter);
                        if (range?.Count != 2)
                            continue;
                        
                        switch (column.GeneralDataType)
                        {
                            case GeneralDataType.Date:
                            case GeneralDataType.DateTime:
                            {
                                var startDate = GetDate(range[0]);
                                if (startDate.HasValue)
                                {
                                    if (column.GeneralDataType == GeneralDataType.Date)
                                        startDate = startDate.Value.StartOfTheDay();

                                    filterCriteria.AddParameterisedCriterion($"ent.{column.PropertyName} >= {{0}}", startDate.Value);
                                }
                                
                                var endDate = GetDate(range[1]);
                                if (endDate.HasValue)
                                {
                                    if (column.GeneralDataType == GeneralDataType.Date)
                                        endDate = endDate.Value.EndOfTheDay();

                                    filterCriteria.AddParameterisedCriterion($"ent.{column.PropertyName} <= {{0}}", endDate.Value);
                                }

                                break;
                            }
                            case GeneralDataType.Time:
                            {
                                var startTime = GetTime(range[0]);
                                if (startTime.HasValue)
                                {
                                    filterCriteria.AddParameterisedCriterion($"ent.{column.PropertyName} >= {{0}}", startTime.Value.StripSeconds());
                                }

                                var endTime = GetTime(range[1]);
                                if (endTime.HasValue)
                                {
                                    filterCriteria.AddParameterisedCriterion($"ent.{column.PropertyName} < {{0}}", endTime.Value.StripSeconds().Add(TimeSpan.FromMinutes(1)));
                                }

                                break;
                            }
                            case GeneralDataType.Numeric:
                            {
                                var startValue = GetDecimal(range[0]);
                                if (startValue.HasValue)
                                    filterCriteria.AddParameterisedCriterion("ent." + column.PropertyName + " >= {0}", startValue.Value);

                                var endValue = GetDecimal(range[1]);
                                if (endValue.HasValue)
                                    filterCriteria.AddParameterisedCriterion("ent." + column.PropertyName + " <= {0}", endValue.Value);
                                break;
                            }
                        }
                        break;
                    }
                    case FilterOperations.Before:
                    {
                        switch (column.GeneralDataType)
                        {
                            case GeneralDataType.Date:
                            case GeneralDataType.DateTime:
                            {
                                // date
                                var dateValue = GetDate(filter.Filter);

                                if (dateValue.HasValue)
                                {
                                    if (column.GeneralDataType == GeneralDataType.Date)
                                        dateValue = dateValue.Value.StartOfTheDay();

                                    filterCriteria.AddParameterisedCriterion("ent." + column.PropertyName + " < {0}", dateValue.Value);
                                }

                                break;
                            }
                            case GeneralDataType.Time:
                            {
                                var timeValue = GetTime(filter.Filter);
                                if (timeValue.HasValue)
                                {
                                    filterCriteria.AddParameterisedCriterion($"ent.{column.PropertyName} < {{0}}",
                                        timeValue.Value.StripSeconds());
                                }

                                break;
                            }
                        }

                        break;
                    }
                    case FilterOperations.After:
                    {
                        switch (column.GeneralDataType)
                        {
                            case GeneralDataType.Date:
                            case GeneralDataType.DateTime:
                            {
                                // date
                                var dateValue = GetDate(filter.Filter);
                                if (dateValue.HasValue)
                                {
                                    if (column.GeneralDataType == GeneralDataType.Date)
                                        dateValue = dateValue.Value.EndOfTheDay();

                                    filterCriteria.AddParameterisedCriterion("ent." + column.PropertyName + " > {0}",
                                        dateValue.Value);
                                }

                                break;
                            }
                            case GeneralDataType.Time:
                            {
                                var timeValue = GetTime(filter.Filter);
                                if (timeValue.HasValue)
                                {
                                    filterCriteria.AddParameterisedCriterion($"ent.{column.PropertyName} > {{0}}",
                                        timeValue.Value.StripSeconds());
                                }

                                break;
                            }
                        }

                        break;
                    }
                }
            }
        }

        private static Person GetCurrentUser()
        {
            var abpSession = StaticContext.IocManager.Resolve<IAbpSession>();
            if (abpSession.UserId == null)
                return null;

            var personService = StaticContext.IocManager.Resolve<IRepository<Person, Guid>>();
            var person = personService.FirstOrDefault(c => c.User.Id == abpSession.GetUserId());
            return person;
        }

        private string GetChildEntitySortProperty(DataTablesDisplayPropertyColumn column) 
        {
            if (column?.GeneralDataType != GeneralDataType.EntityReference)
                return null;

            if (string.IsNullOrWhiteSpace(column.EntityReferenceTypeShortAlias))
                return null;

            return _entityConfigStore.Get(column.EntityReferenceTypeShortAlias)?.DisplayNamePropertyInfo?.Name ?? "Id";
        }

        private void AppendOrderBy(DataTableQueryBuildingContext context, bool userSortingDisabled)
        {
            var columnsToSort = !userSortingDisabled && context.DataTableInput.Sorting.Any()
                ? context.DataTableInput.Sorting.Select(sc =>
                    {
                        var column = context.Columns.FirstOrDefault(c => c.PropertyName == sc.Id && c.IsSortable) as DataTablesDisplayPropertyColumn;

                        return new SortingInfo
                        {
                            Column = column,
                            SortOrder = sc.Desc ? "desc" : "asc",
                        };
                    })
                    .Where(i => i.Column != null)
                    .ToList()
                : context.Columns.OfType<DataTablesDisplayPropertyColumn>().Select(c => c.DefaultSorting != null
                        ? new SortingInfo()
                        {
                            Column = c,
                            SortOrder = c.DefaultSorting == ListSortDirection.Descending ? "desc" : "asc",
                        }
                        : null)
                    .Where(i => i?.Column != null)
                    .ToList();

            var sortItems = new List<string>();

            foreach (var columnToSort in columnsToSort)
            {
                var alias = "ent";

                // add joins
                var propertyName = string.Empty;
                var parts = columnToSort.Column.PropertyName.Split(".");

                var currentClass = context.RootClass;
                var currentPath = alias;

                for (int i = 0; i < parts.Length; i++)
                {
                    var part = parts[i];
                    var nestedProp = currentClass.GetProperty(part);
                    currentClass = nestedProp?.PropertyType;

                    currentPath += "." + part;

                    if (currentClass.IsEntityType())
                    {
                        var join = context.Joins.FirstOrDefault(j => j.Reference == currentPath);
                        if (join == null)
                        {
                            join = new JoinClause
                            {
                                Reference = currentPath,
                                Alias = currentPath.Replace('.', '_'),
                                JoinType = JoinType.Left
                            };
                            context.Joins.Add(join);
                        }
                        alias = join.Alias;
                        currentPath = alias;
                    }

                    if (i == parts.Length - 1) 
                    {
                        if (currentClass.IsEntityType())
                        {
                            propertyName = _entityConfigStore.Get(currentClass)?.DisplayNamePropertyInfo?.Name;
                            if (string.IsNullOrWhiteSpace(propertyName))
                                propertyName = currentClass is IHasCreationTime
                                    ? nameof(IHasCreationTime.CreationTime)
                                    : nameof(IEntity.Id);
                        }
                        else {
                            var refListIdentifier = nestedProp.GetReferenceListIdentifierOrNull();
                            if (refListIdentifier != null) 
                            {
                                var refListAlias = currentPath.Replace('.', '_');
                                var refListJoin = new JoinClause
                                {
                                    Reference = nameof(FlatReferenceListItem),
                                    Alias = refListAlias,
                                    JoinType = JoinType.Left,
                                    Condition = $"{refListAlias}.{nameof(FlatReferenceListItem.ItemValue)} = {currentPath} and {refListAlias}.{nameof(FlatReferenceListItem.ReferenceListFullName)} = '{refListIdentifier.Namespace}.{refListIdentifier.Name}'"
                                };
                                context.Joins.Add(refListJoin);
                                alias = refListJoin.Alias;
                                propertyName = nameof(FlatReferenceListItem.Item);
                            } else
                                propertyName = part;
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(propertyName))
                    throw new Exception("Failed to find sorting property");

                sortItems.Add($"{alias}.{propertyName} {columnToSort.SortOrder}");
            }

            context.OrderBy = sortItems.Delimited(", ");
        }

        /// <summary>
        /// Apply all selected stored filters
        /// </summary>
        private void AppendPredefinedFilters(DataTableQueryBuildingContext queryContext)
            // [NotNull] List<DataTableColumn> columns, [NotNull] DataTableGetDataInput input, [NotNull] FilterCriteria filterCriteria
        {
            if (queryContext.DataTableInput.SelectedFilters == null)
                return;

            // Validate stored filters: validate IDs then also validate that for Exclusive filters, user can only submit zero or 1 filters to apply
            foreach (var filter in queryContext.DataTableInput.SelectedFilters)
            {
                if (filter.Expression == null || string.IsNullOrWhiteSpace(filter.Expression?.ToString()))
                    continue;

                // todo: migrate tags replacer from G2 reporting framework. For now, we only replace {userId}
                var tagsDictionary = new Dictionary<string, string>
                {
                    {"{userId}", StaticContext.IocManager.Resolve<IAbpSession>().UserId.ToString()}
                };

                switch (filter.ExpressionType?.ToLower())
                {
                    //case RefListFilterExpressionType.JsonLogic:
                    case "jsonlogic":
                    default:
                        {
                            // JsonLogic is converted to HQL
                            var jsonLogic = JObject.Parse(filter.Expression?.ToString());

                            // convert json logic to HQL
                            var context = new JsonLogic2HqlConverterContext();
                            
                            if (!string.IsNullOrWhiteSpace(queryContext.DataTableInput.EntityType))
                            {
                                var entityConfig = _entityConfigStore.Get(queryContext.DataTableInput.EntityType);
                                var properties = _metadataProvider.GetProperties(entityConfig.EntityType);

                                DataTableHelper.FillVariablesResolvers(properties, context);
                                DataTableHelper.FillContextMetadata(properties, context);
                            }
                            else 
                            {
                                DataTableHelper.FillVariablesResolvers(queryContext.Columns, context);
                                DataTableHelper.FillContextMetadata(queryContext.Columns, context);
                            }

                            var hql = _jsonLogic2HqlConverter.Convert(jsonLogic, context);

                            queryContext.FilterCriteria.FilterClauses.Add(hql);
                            foreach (var parameter in context.FilterParameters)
                            {
                                queryContext.FilterCriteria.FilterParameters.Add(parameter.Key, parameter.Value);
                            }
                            
                            break;
                        }

                    // HQL is default
                    case "hql":
                        {
                            var hql = filter.Expression?.ToString();
                            foreach (var tag in tagsDictionary)
                                if (hql.Contains(tag.Key))
                                    hql = hql.Replace(tag.Key, tag.Value);
                            queryContext.FilterCriteria.FilterClauses.Add(hql);
                            // Use parameters instead of replacing tags
                            break;
                        }
                }
            }
        }


        /// <summary>
        /// Apply all selected stored filters
        /// </summary>
        private void AppendStoredFilters([NotNull] DataTableConfig tableConfig, [NotNull] DataTableQueryBuildingContext queryContext)
        {
            var entityConfigurationStore = _iocResolver.Resolve<IEntityConfigurationStore>();
            var filterIds = queryContext.DataTableInput.SelectedStoredFilterIds
                .Where(id => queryContext.DataTableInput.SelectedFilters == null || !queryContext.DataTableInput.SelectedFilters.Any(f => f.Id == id))
                .Select(id => id.ToGuid())
                .Where(id => id != Guid.Empty)
                .ToList();

            var filterCriteria = queryContext.FilterCriteria;

            // Validate stored filters: validate IDs then also validate that for Exclusive filters, user can only submit zero or 1 filters to apply
            foreach (var filterId in filterIds)
            {
                // if filter included into request - skip, it'll be handled by another handler
                if (queryContext.DataTableInput.SelectedFilters != null && queryContext.DataTableInput.SelectedFilters.Any(f => f.Id == filterId.ToString()))
                    continue;

                var storedFilter = _filterRepository.Get(filterId);
                if (storedFilter.IsExclusive && queryContext.DataTableInput.SelectedStoredFilterIds.Count > 1)
                    throw new Exception($"Only one Exclusive filter can be selected. Please either ensure one filter is selected or update {storedFilter.Name} filter with ID {storedFilter.Id} to not be exclusive");
                
                // Security: when visibility conditions are provided, restrict the filter
                if (storedFilter.VisibleBy.Any())
                {
                    var shaRoleType = entityConfigurationStore.Get(typeof(ShaRole))?.TypeShortAlias;
                    var visibleByRoles = storedFilter.VisibleBy.Where(v => v.OwnerType == shaRoleType)
                        .Select(v => _roleRepository.Get(v.OwnerId.ToGuid())).ToList();
                    var hasAccess = false;
                    var currentUser = GetCurrentUser();
                    foreach (var role in visibleByRoles)
                    {
                        if (_rolePersonRepository.GetAll().Any(c => c.Role == role && c.Person == currentUser))
                        {
                            hasAccess = true;
                            break;
                        }
                    }

                    if (!hasAccess)
                    {
                        // Log the issue
                        Logger.Error($"User has no access to {storedFilter.Name} filter with ID {storedFilter.Id}");
                        // Add "1=0" clause for no data to be shown.
                        filterCriteria.FilterClauses.Add("1=0");
                    }
                }

                // todo: migrate tags replacer from G2 reporting framework. For now, we only replace {userId}
                var tagsDictionary = new Dictionary<string, string>
                {
                    {"{userId}", StaticContext.IocManager.Resolve<IAbpSession>().UserId.ToString()}
                };

                switch (storedFilter.ExpressionType)
                {
                    case RefListFilterExpressionType.JsonLogic:
                    {
                        // JsonLogic is converted to HQL
                        var jsonLogic = JObject.Parse(storedFilter.Expression);

                        // convert json logic to HQL
                        var context = new JsonLogic2HqlConverterContext();
                        DataTableHelper.FillVariablesResolvers(tableConfig.Columns, context);
                        DataTableHelper.FillContextMetadata(tableConfig.Columns, context);

                        var hql = _jsonLogic2HqlConverter.Convert(jsonLogic, context);

                        filterCriteria.FilterClauses.Add(hql);
                        foreach (var parameter in context.FilterParameters)
                        {
                            filterCriteria.FilterParameters.Add(parameter.Key, parameter.Value);
                        }
                        break;
                    }

                    case RefListFilterExpressionType.Column:
                        throw new NotImplementedException("This can only be used for reports. Please use HQL or Query Builder instead");
                    
                    // HQL is default
                    case RefListFilterExpressionType.Hql:
                    default:
                    {
                        var hql = storedFilter.Expression;
                        foreach (var tag in tagsDictionary)
                            if (hql.Contains(tag.Key))
                                hql = hql.Replace(tag.Key, tag.Value);
                        filterCriteria.FilterClauses.Add(hql);
                        // Use parameters instead of replacing tags
                        break;
                    }
                }
            }
        }

        #region Json conversion.Mathods support both Newtonsoft.Json and System.Text.Json. For now we use Newtonsoft because a lot of things are still missing in the System.Text.Json

        private List<object> GetArray(object value)
        {
            if (value is JArray jArray)
            {
                return jArray.AsEnumerable().Cast<object>().ToList();
            }

            if (value is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
            {
                return jsonElement.EnumerateArray().Cast<object>().ToList();
            }

            return new List<object>();
        }

        private string GetString(object value)
        {
            if (value is JsonElement jsonElement)
                return jsonElement.ValueKind != JsonValueKind.Null
                    ? jsonElement.GetString()
                    : null;
            
            return value.ToString();
        }

        private bool? GetBoolean(object value)
        {
            if (value is JsonElement jsonElement)
                return jsonElement.ValueKind != JsonValueKind.Null
                    ? jsonElement.GetBoolean()
                    : (bool?)null;

            return value is bool boolValue
                ? boolValue
                : (bool?)null;
        }

        private decimal? GetDecimal(object value)
        {
            if (value is JsonElement jsonElement)
                return jsonElement.ValueKind != JsonValueKind.Null && jsonElement.TryGetDecimal(out var decimalValue)
                    ? decimalValue
                    : (decimal?)null;

            var unwrapped = value is JValue jValue
                ? jValue.Value
                : value;

            if (unwrapped is double doubleValue)
                return Convert.ToDecimal(doubleValue);

            if (unwrapped is Int64 longValue)
                return Convert.ToDecimal(longValue);

            if (unwrapped is int intValue)
                return Convert.ToDecimal(intValue);

            if (unwrapped is decimal decValue)
                return decValue;

            return null;
        }

        private DateTime? GetDate(object value)
        {
            return GetDate(value, out _);
        }

        private DateTime? GetDate(object value, out bool containsTime)
        {
            containsTime = false;
            if (value is JsonElement jsonElement)
                return jsonElement.ValueKind != JsonValueKind.Null && jsonElement.TryGetDateTime(out var dateValue)
                    ? dateValue
                    : (DateTime?)null;

            var stringValue = value is string strValue
                ? strValue
                : value is JValue jValue
                    ? jValue.Value?.ToString()
                    : null;

            var formats = new List<string>
            {
                "dd/MM/yyyy HH:mm:ss",
                "dd/MM/yyyy HH:mm",
                "dd/MM/yyyy"
            };

            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(stringValue, format, CultureInfo.InvariantCulture, DateTimeStyles.None,
                    out var date))
                {
                    containsTime = format.Contains("HH:mm");
                    return date;
                }
            }
            
            return null;
        }

        private TimeSpan? GetTime(object value)
        {
            var stringValue = value is string strValue
                ? strValue
                : value is JValue jValue
                    ? jValue.Value?.ToString()
                    : null;

            var formats = new List<string>
            {
                @"hh\:mm\:ss",
                @"hh\:mm"
            };

            foreach (var format in formats)
            {
                var time = Parser.ParseTime(stringValue, format);
                if (time != null)
                    return time;
            }

            return null;
        }


        #endregion

        #region HQL part - to be reviewed

        public async Task<IList<TEntity>> FindAllAsync<TEntity, TPrimaryKey>(FilterCriteria criteria, int skip, int take, string orderBy, CancellationToken cancellationToken) where TEntity : class, IEntity<TPrimaryKey>
        {
            if (criteria.FilteringMethod != FilterCriteria.FilterMethod.Hql)
                throw new NotImplementedException();

            return await FindAllHqlAsync<TEntity>(criteria, skip, take, orderBy, cancellationToken);
        }

        public async Task<IList<TEntity>> FindAllAsync<TEntity, TPrimaryKey>(DataTableQueryBuildingContext queryContext, int skip, int take, string orderBy, CancellationToken cancellationToken) where TEntity : class, IEntity<TPrimaryKey>
        {
            if (queryContext.FilterCriteria.FilteringMethod != FilterCriteria.FilterMethod.Hql)
                throw new NotImplementedException();

            return await FindAllHqlAsync<TEntity>(queryContext, skip, take, cancellationToken);
        }
        
        private async Task<IList<TEntity>> FindAllHqlAsync<TEntity>(FilterCriteria criteria, int skip, int take, string orderBy, CancellationToken cancellationToken)
        {
            var q = CreateQueryHql<TEntity>(criteria, orderBy);

            if (skip > 0)
                q.SetFirstResult(skip);

            if (take > 0)
                q.SetMaxResults(take);

            return await q.ListAsync<TEntity>(cancellationToken);
        }

        private async Task<IList<TEntity>> FindAllHqlAsync<TEntity>(QueryBuildingContext queryContext, int skip, int take, CancellationToken cancellationToken)
        {
            var q = CreateQueryHql<TEntity>(queryContext);

            if (skip > 0)
                q.SetFirstResult(skip);

            if (take > 0)
                q.SetMaxResults(take);

            return await q.ListAsync<TEntity>(cancellationToken);
        }

        private IQuery CreateQueryHql<TEntity>(FilterCriteria criteria, string orderBy = null)
        {
            var sessionFactory = _iocResolver.Resolve<ISessionFactory>();
            var session = sessionFactory.GetCurrentSession();

            var q = string.IsNullOrEmpty(orderBy)
                ? session.CreateQuery(typeof(TEntity), criteria)
                : session.CreateQuery(typeof(TEntity), criteria, orderBy);

            return q;
        }

        private IQuery CreateQueryHql<TEntity>(QueryBuildingContext queryContext)
        {
            var sessionFactory = _iocResolver.Resolve<ISessionFactory>();
            var session = sessionFactory.GetCurrentSession();

            return session.CreateQuery(queryContext);
        }

        private IQuery CreateQueryCountHql<TEntity>(DataTableQueryBuildingContext queryContext)
        {
            var sessionFactory = _iocResolver.Resolve<ISessionFactory>();
            var session = sessionFactory.GetCurrentSession();

            var q = session.CreateQueryCount(typeof(TEntity), queryContext);

            return q;
        }

        public async Task<Int64> CountAsync<TEntity, TPrimaryKey>(DataTableQueryBuildingContext queryContext, CancellationToken cancellationToken) where TEntity : class, IEntity<TPrimaryKey>
        {
            var query = CreateQueryCountHql<TEntity>(queryContext);
            return await query.UniqueResultAsync<Int64>(cancellationToken);
        }


        #endregion

        private bool IsColumnVisibleOnClient(string columnName, DataTableGetDataInput dataTableParam)
        {
            return true;
            /*
            if (string.IsNullOrWhiteSpace(columnName))
                return false;
            var indexOnClient = dataTableParam.sName.IndexOf(columnName);
            return indexOnClient > -1 && indexOnClient <= dataTableParam.bVisible.Count - 1 && dataTableParam.bVisible[indexOnClient];
            */
        }

        /// <summary>
        /// Gets the required Order By clause based on the dataTableParam
        /// </summary>
        /// <returns>Returns the relevant Order By clause to apply when querying the Db through
        /// a <typeparamref name="Shesha.Framework.Data.NHibernate.Repository"/>.</returns>
        private string GetOrderByClause(List<DataTableColumn> columns, DataTableGetDataInput input, bool userSortingDisabled)
        {
            var entityConfigurationStore = _iocResolver.Resolve<IEntityConfigurationStore>();
            var columnsToSort = !userSortingDisabled && input.Sorting.Any()
                ? input.Sorting.Select(sc =>
                    {
                        var column = columns.FirstOrDefault(c => c.PropertyName == sc.Id && c.IsSortable) as DataTablesDisplayPropertyColumn;
                        var childEntityDisplayName = column?.GeneralDataType == GeneralDataType.EntityReference &&
                                                     !string.IsNullOrWhiteSpace(column.EntityReferenceTypeShortAlias)
                            ? entityConfigurationStore.Get(column.EntityReferenceTypeShortAlias)?.DisplayNamePropertyInfo
                            : null;

                        return new SortingInfo
                        {
                            Column = column,
                            SortOrder = sc.Desc ? "desc" : "asc",
                            ChildEntityDisplayProperty = childEntityDisplayName?.Name
                        };
                    })
                    .Where(i => i.Column != null)
                    .ToList()
                : columns.Select(c => c.DefaultSorting != null 
                        ? new SortingInfo()
                        {
                            Column = c as DataTablesDisplayPropertyColumn,
                            SortOrder = c.DefaultSorting == ListSortDirection.Descending ? "desc" : "asc",
                            ChildEntityDisplayProperty = null
                        }
                        : null)
                    .Where(i => i?.Column != null)
                    .ToList();

            // 'ent.'-prefix helps by removing ambiguity in cases where query joins other tables/entities which may also have properties with the same name
            var sortString = columnsToSort.Select(i => string.IsNullOrWhiteSpace(i.ChildEntityDisplayProperty)
                ? $"ent.{i.Column.PropertyName} {i.SortOrder}"
                : $"ent.{i.Column.PropertyName}.{i.ChildEntityDisplayProperty} {i.SortOrder}").Delimited(", ");

            return sortString;
        }

        private class SortingInfo // todo: refactor and remove
        {
            public DataTablesDisplayPropertyColumn Column { get; set; }
            public string SortOrder { get; set; }
            public string ChildEntityDisplayProperty { get; set; }
        }
    }
}
