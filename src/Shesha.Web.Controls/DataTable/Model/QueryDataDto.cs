using System;
using System.Collections;
using System.Collections.Generic;
using Abp.Domain.Entities;

namespace Shesha.Web.DataTable.Model
{
    public class QueryDataDto<TEntity, TPrimaryKey>: IQueryDataDto where TEntity : class, IEntity<TPrimaryKey>
    {
        /// <summary>
        /// Total number of rows after filters
        /// </summary>
        public Int64 TotalRows { get; set; }

        /// <summary>
        /// Total number of rows before filters
        /// </summary>
        public Int64 TotalRowsBeforeFilter { get; set; }

        public IList Rows => Entities as IList;

        public IList<TEntity> Entities { get; set; }
    }

    public interface IQueryDataDto
    {
        /// <summary>
        /// Total number of rows after filters
        /// </summary>
        Int64 TotalRows { get; }

        /// <summary>
        /// Total number of rows before filters
        /// </summary>
        Int64 TotalRowsBeforeFilter { get; }

        public IList Rows { get; }
    }
}
