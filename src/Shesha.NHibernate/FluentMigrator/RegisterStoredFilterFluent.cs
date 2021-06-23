using System;
using System.Collections.Generic;
using System.Linq;
using FluentMigrator;
using FluentMigrator.Infrastructure;

namespace Shesha.FluentMigrator
{
    /// <summary>
    /// Saved filter registration helper interface
    /// </summary>
    public class RegisterStoredFilterFluent : IFluentSyntax
    {
        private Migration _migration;
        private Guid _id;
        private string _filterName;
        private string _hqlExpression;

        private string _namespace;
        private string _description;

        private int? _orderIndex;
        private bool _isExclusive;

        private List<string> _containingDataTables = new List<string>();
        private List<Guid> _containingReports = new List<Guid>();
        private List<(string @entityType, string entityId)> _containingOtherEntities = new List<(string @entityType, string entityId)>();
        
        private List<string> _visibilityPersons = new List<string>();
        private List<(string @namespace, string name)> _visibilityRoles = new List<(string @namespace, string name)>();
        private List<(string @entityType, string entityId)> _visibilityEntities = new List<(string @entityType, string entityId)>();

        /// <summary>
        /// Basic filters only need name and HQL expression. The rest can be added with Fluent helpers when needed
        /// </summary>
        /// <param name="migration"></param>
        /// <param name="filterName">Filter name to display to user</param>
        /// <param name="hqlExpression">HQL expression. Example for My Items filter: `ent.CreatorUserId={userId}`</param>
        public RegisterStoredFilterFluent(Migration migration, string filterName, string hqlExpression)
        {
            _migration = migration;
            _id = Guid.NewGuid();
            _isExclusive = true;
            _filterName = filterName;
            _hqlExpression = hqlExpression;
        }

        /// <summary>
        /// Basic filters only need name and HQL expression. The rest can be added with Fluent helpers when needed
        /// </summary>
        /// <param name="migration"></param>
        /// <param name="filterName">Filter name to display to user</param>
        /// <param name="hqlExpression">HQL expression. Example for My Items filter: `ent.CreatorUserId={userId}`</param>
        /// <param name="isExclusive">It's true by default. Only set it to false if the filter selection dropdown must support multi-selection</param>
        public RegisterStoredFilterFluent(Migration migration, string filterName, string hqlExpression, bool isExclusive) : this(migration, filterName, hqlExpression)
        {
            _isExclusive = isExclusive;
        }

        /// <summary>
        /// Basic filters only need name and HQL expression. The rest can be added with Fluent helpers when needed
        /// </summary>
        /// <param name="migration"></param>
        /// <param name="filterName">Filter name to display to user</param>
        /// <param name="hqlExpression">HQL expression. Example for My Items filter: `ent.CreatorUserId={userId}`</param>
        /// <param name="isExclusive">It's true by default. Only set it to false if the filter selection dropdown must support multi-selection</param>
        /// <param name="orderIndex">Order index within a container. Please use it when alphabetical ordering is not what you need</param>
        public RegisterStoredFilterFluent(Migration migration, string filterName, string hqlExpression, bool isExclusive, int? orderIndex) : this(migration, filterName, hqlExpression, isExclusive)
        {
            _orderIndex = orderIndex;
        }
        
        /// <summary>
        /// Only use when the filter ID must be set explicitly rather than generated e.g. when using code filters (see ICustomStoredFilterRegistration)
        /// </summary>
        public RegisterStoredFilterFluent WithExplicitId(Guid filterId)
        {
            _id = filterId;
            return this;
        }

        /// <summary>
        /// Only use when ID must be set explicitly rather than generated
        /// </summary>
        public RegisterStoredFilterFluent WithNamespace(string @namespace)
        {
            _namespace = @namespace;
            return this;
        }

        /// <summary>
        /// Links current filter to one or more data tables
        /// </summary>
        public RegisterStoredFilterFluent OnDataTables(params string[] dataTableId)
        {
            _containingDataTables.AddRange(dataTableId);
            return this;
        }

        /// <summary>
        /// Links current filter to report(s)
        /// </summary>
        public RegisterStoredFilterFluent OnReports(params Guid[] reportIds)
        {
            _containingReports.AddRange(reportIds);
            return this;
        }

        /// <summary>
        /// Links current filter to other entity(ies)
        /// </summary>
        public RegisterStoredFilterFluent OnEntities(params (string @entityType, string entityId)[] entities)
        {
            _containingOtherEntities.AddRange(entities);
            return this;
        }

        /// <summary>
        /// Makes the filter visible to given role(s)
        /// Note: ExecuteScalar is missing yet so we require person ID not username for now
        /// </summary>
        public RegisterStoredFilterFluent VisibleToPersons(params string[] usernames)
        {
            _visibilityPersons.AddRange(usernames);
            return this;
        }

        /// <summary>
        /// Makes the filter visible to given role(s).
        /// Note: ExecuteScalar is missing yet so we require role ID not name for now
        /// </summary>
        public RegisterStoredFilterFluent VisibleToRoles(params (string @namespace, string name)[] roles)
        {
            _visibilityRoles.AddRange(roles);
            return this;
        }

        /// <summary>
        /// List of entities that the filter should be visible to.
        /// </summary>
        public RegisterStoredFilterFluent VisibleToEntities(params (string @entityType, string entityId)[] entities)
        {
            _visibilityEntities.AddRange(entities);
            return this;
        }

        private string GetAvailableColumn(string table, params string[] columns) 
        {
            return columns.FirstOrDefault(c => _migration.Schema.Table(table).Column(c).Exists());
        }

        public void Execute()
        {
            var expressionColumn = GetAvailableColumn("Frwk_StoredFilters", "HqlExpression", "Expression");
            var expressionTypeColumn = GetAvailableColumn("Frwk_StoredFilters", "StoredFilterTypeLkp", "ExpressionTypeLkp");
            
            _migration.Insert.IntoTable("Frwk_StoredFilters").InSchema("dbo")
                .Row(new Dictionary<string, object>
                {
                    {"Id", _id},
                    {"Name", _filterName},
                    {"Namespace", _namespace},
                    {expressionColumn, _hqlExpression},
                    {"IsExclusive", _isExclusive},
                    {"Description", _description},
                    {"OrderIndex", _orderIndex},
                    {expressionTypeColumn, 1}
                });

            // Containers: merge all into 1 list and insert
            _containingOtherEntities = _containingOtherEntities
                .Concat(_containingReports.Select(reportId => ("Shesha.Core.ReportDefinition", reportId.ToString())))
                .Concat(_containingDataTables.Select(tableId => ("", tableId.ToString())))
                .ToList();
            foreach (var container in _containingOtherEntities)
            {
                _migration.Insert.IntoTable("Frwk_StoredFilterContainers").InSchema("dbo")
                    .Row(new Dictionary<string, object>
                    {
                        {"Id", Guid.NewGuid()},
                        {"FilterId", _id},
                        {"Frwk_OwnerType", container.entityType},
                        {"Frwk_OwnerId", container.entityId},
                        {"IsHidden", 0},
                        {"IsDefaultFilter", 0}
                    });
            }

            // Entity visibility: merge all into 1 list and insert
            _visibilityEntities = _visibilityEntities
                .Concat(_visibilityRoles.Select(role => ("Shesha.Core.ShaRole", role.name))) // ExecuteScalar is missing yet so we require person ID not username
                .Concat(_visibilityPersons.Select(username => ("Shesha.Core.Person", username))) // ExecuteScalar is missing yet so we require person ID not username
                .ToList();

            foreach (var visibility in _visibilityEntities)
            {
                _migration.Insert.IntoTable("Frwk_EntityVisibility").InSchema("dbo")
                    .Row(new Dictionary<string, object>
                    {
                        {"Id", Guid.NewGuid()},

                        // Filter id 
                        {"EntityType", "Shesha.Framework.StoredFilter"},
                        {"EntityId", _id},

                        // Role id
                        {"Frwk_OwnerType", visibility.entityType},
                        {"Frwk_OwnerId", visibility.entityId},

                        {"EntityAccessLkp", (int) Domain.Enums.RefListEntityAccess.FullAccess}
                    });
            }
        }
    }
}
