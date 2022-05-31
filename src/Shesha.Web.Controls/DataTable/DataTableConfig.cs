﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Abp.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Shesha.Configuration.Runtime;
using Shesha.Domain;
using Shesha.Domain.Attributes;
using Shesha.Extensions;
using Shesha.Reflection;
using Shesha.Services;
using Shesha.Utilities;
using Shesha.Web.DataTable.Columns;

namespace Shesha.Web.DataTable
{
    /// <summary>
    /// Configuration of data table
    /// </summary>
    public class DataTableConfig: IDataTableConfig
    {
        /// <summary>
        /// 
        /// </summary>
        public DataTableConfig(string id)
        {
            Id = id;
            Columns = new List<DataTableColumn>();
            
            PageSize = 10;
            QuickSearchMode = QuickSearchMode.Sql;
        }

        /// <summary>
        /// Unique Id of the table
        /// </summary>
        public string Id { get; protected set; }

        /// <summary>
        /// If true, API returns DTOs for complex types (entity reference, referencelists etc.) instead of text values
        /// </summary>
        public bool UseDtos { get; set; }

        #region CRUD support

        /// <summary>
        /// Create url
        /// </summary>
        public Func<IUrlHelper, string> CreateUrl { get; set; }

        /// <summary>
        /// Details url
        /// </summary>
        public Func<IUrlHelper, string> DetailsUrl { get; set; }

        /// <summary>
        /// Update url
        /// </summary>
        public Func<IUrlHelper, string> UpdateUrl { get; set; }

        /// <summary>
        /// Delete url
        /// </summary>
        public Func<IUrlHelper, string> DeleteUrl { get; set; }

        #endregion

        /// <summary>
        /// Page size
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Indicates is user sorting disabled or not
        /// </summary>
        public bool UserSortingDisabled { get; set; }

        /// <summary>
        /// Type of the row
        /// </summary>
        [JsonIgnore]
        public Type RowType { get; set; }

        /// <summary>
        /// Id type of the row
        /// </summary>
        public Type IdType { get; set; }

        /// <summary>
        /// Table columns
        /// </summary>
        public List<DataTableColumn> Columns { get; set; }

        /// <summary>
        /// List of stored filters to show (populated from DB and from attributes)
        /// </summary>
        public List<DataTableStoredFilter> StoredFilters { get; set; } = new List<DataTableStoredFilter>();

        /// <summary>
        /// List of code filters
        /// </summary>
        public List<ICustomStoredFilterRegistration> CodeFilters { get; protected set; } = new List<ICustomStoredFilterRegistration>();

        /// <summary>
        /// 
        /// </summary>
        [JsonIgnore]
        public List<DataTableColumn> AuthorizedColumns => Columns; // todo: review the design and decide do we need it, for now just return all columns

        /// <summary>
        /// Defines a filtration logic according to the quick search input
        /// Where: 
        /// FilterCriteria - are the filterCriteria to be updated with with filtering logic
        /// QuickSearch - value of the quick search input
        /// </summary>
        [JsonIgnore]
        public Action<FilterCriteria, string> OnRequestToQuickSearch { get; set; }

        /// <summary>
        /// Defines a filtration logic according to the user's input
        /// Where: 
        /// FilterCriteria - are the filterCriteria to be updated with with filtering logic
        /// NameValueCollection - are values coming from the request/form. 
        /// </summary>
        [JsonIgnore]
        public Action<FilterCriteria, DataTableGetDataInput> OnRequestToFilter { get; set; }

        /// <summary>
        /// Defines a filtration logic according to the user's input
        /// Where: 
        /// FilterCriteria - are the filterCriteria to be updated with with filtering logic
        /// NameValueCollection - are values coming from the request/form. 
        /// </summary>
        [JsonIgnore]
        public Func<FilterCriteria, DataTableGetDataInput, Task> OnRequestToFilterAsync { get; set; }

        /// <summary>
        /// Defines a filtration logic that should be applied to the table independently of the user filtration
        /// Also it's used to calculate rows count for the following label: "Showing 0 to 0 of 0 entries (filtered from XXX total entries)"
        /// </summary>
        [JsonIgnore]
        public Action<FilterCriteria, DataTableGetDataInput> OnRequestToFilterStatic { get; set; }

        /// <summary>
        /// Defines a filtration logic that should be applied to the table independently of the user filtration
        /// Also it's used to calculate rows count for the following label: "Showing 0 to 0 of 0 entries (filtered from XXX total entries)"
        /// </summary>
        [JsonIgnore]
        public Func<FilterCriteria, DataTableGetDataInput, Task> OnRequestToFilterStaticAsync { get; set; }

        /// <summary>
        /// Quick search mode
        /// </summary>
        public QuickSearchMode QuickSearchMode { get; set; }

        /// <summary>
        /// Registration by class (this way of registration is preferred for filters that are available on more than 1 data table)
        /// </summary>
        /// <typeparam name="TFilter"></typeparam>
        public virtual void RegisterStoredFilter<TFilter>() where TFilter : ICustomStoredFilterRegistration
        {
            var filterRegistration = Activator.CreateInstance<TFilter>();
            CodeFilters.Add(filterRegistration);
        }
    }

    /// <summary>
    /// Datatable configuration
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TId"></typeparam>
    public class DataTableConfig<T, TId> : DataTableConfig where T : class/*, IEntity<TId>*/
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="id"></param>
        public DataTableConfig(string id)
            : base(id)
        {
            RowType = typeof(T);
            IdType = typeof(TId);

            // Add Id column to each configuration by default
            if (RowType.GetProperty(nameof(IEntity.Id)) != null)
                AddProperty(nameof(IEntity.Id), nameof(IEntity.Id), c => c.Visible(false).IsFilterable(false).Exportable(false));
        }

        private EntityConfiguration _entityConfig;

        /// <summary>
        /// Entity configuration
        /// </summary>
        [JsonIgnore]
        protected EntityConfiguration EntityConfig
        {
            get
            {
                return _entityConfig ??= typeof(T).GetEntityConfiguration();
            }
        }

        /// <summary>
        /// Add custom column
        /// </summary>
        /// <param name="name">Column name</param>
        /// <param name="property">Value evaluator</param>
        /// <param name="transform">Fluent configuration of the column</param>
        /// <returns></returns>
        public DataTableColumn AddCustomColumn(string name, Func<T, string> property, Action<DataTableColumnFluentConfig> transform = null)
        {
            if (Columns.Any(c => c.PropertyName != null && c.PropertyName.Equals(name, StringComparison.InvariantCultureIgnoreCase)))
                throw new Exception("Column with this name already exists in the table configuration");

            var column = new DataTablesCustomColumn<T>(property)
            {
                DataTableConfig = this,
                PropertyName = name,
                Name = name,
                Caption = name
            };
            transform?.Invoke(column.Fluent);
            Columns.Add(column);
            return column;
        }

        /// <summary>
        /// Add property
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="property">Property expression</param>
        /// <param name="transform">Fluent configuration of the column</param>
        /// <returns></returns>
        public DataTableColumn AddProperty<TValue>(Expression<Func<T, TValue>> property, Action<DataTableColumnFluentConfig> transform = null)
        {
            return AddProperty<TValue>(property, null, transform);
        }

        /// <summary>
        /// Add property
        /// </summary>
        /// <param name="property">Property expression</param>
        /// <param name="transform">Fluent configuration of the column</param>
        /// <returns></returns>
        public DataTableColumn AddProperty(Expression<Func<T, object>> property, Action<DataTableColumnFluentConfig> transform = null)
        {
            return AddProperty<object>(property, null, transform);
        }

        /// <summary>
        /// Add property
        /// </summary>
        public DataTableColumn AddProperty<TValue>(Expression<Func<T, TValue>> property, string displayName,
            Action<DataTableColumnFluentConfig> transform = null)
        {
            var propName =
                property == null
                    ? null
                    : ReflectionHelper.GetPropertyName(property);
            return AddProperty(propName, displayName, transform);
        }

        /// <summary>
        /// Utility method to add a PropertyDisplayTableColumn (which is by far the most common) to the Columns collection.
        /// </summary>
        public virtual DataTableColumn AddProperty(string propName, string displayName, Action<DataTableColumnFluentConfig> transform = null)
        {
            var helper = StaticContext.IocManager.Resolve<IDataTableHelper>();
            var column = helper.GetDisplayPropertyColumn(RowType, propName);
            column.DataTableConfig = this;
            Columns.Add(column);

            if (!string.IsNullOrWhiteSpace(displayName))
                column.Fluent.Caption(displayName);

            // Custom configuration
            transform?.Invoke(column.Fluent);

            return column;
        }

    }
}