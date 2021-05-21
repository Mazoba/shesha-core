using System.Collections.Generic;

namespace Shesha.Web.DataTable.Model
{
    /// <summary>
    /// Datatable configuration DTO
    /// </summary>
    public class DataTableConfigDto
    {
        /// <summary>
        /// Unique identifier of the configuration
        /// </summary>
        public string Id { get; protected set; }
        
        /// <summary>
        /// Default page size
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Columns
        /// </summary>
        public List<DataTableColumnDto> Columns { get; set; }

        /// <summary>
        /// Stored filters
        /// </summary>
        public List<DataTableStoredFilterDto> StoredFilters { get; set; }

        #region CRUD support

        /// <summary>
        /// Create url
        /// </summary>
        public string CreateUrl { get; set; }

        /// <summary>
        /// Details url
        /// </summary>
        public string DetailsUrl { get; set; }

        /// <summary>
        /// Update url
        /// </summary>
        public string UpdateUrl { get; set; }

        /// <summary>
        /// Delete url
        /// </summary>
        public string DeleteUrl { get; set; }

        #endregion
    }
}
