using JetBrains.Annotations;
using Shesha.Domain;
using Shesha.Domain.QueryBuilder;
using System;
using System.Collections.Generic;

namespace Shesha.Web.DataTable.QueryBuilder
{
    /// <summary>
    /// Query building context
    /// </summary>
    public class DataTableQueryBuildingContext: QueryBuildingContext
    {
        [NotNull]
        public List<DataTableColumn> Columns { get; set; }
        [NotNull]
        public DataTableGetDataInput DataTableInput { get; set; }

        public DataTableQueryBuildingContext(Type rootClass, List<DataTableColumn> columns, DataTableGetDataInput dataTableInput): base(rootClass)
        {
            Columns = columns;
            DataTableInput = dataTableInput;
        }
    }
}
