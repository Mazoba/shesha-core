namespace Shesha.Web.DataTable.Model
{
    /// <summary>
    /// Columns filter DTO
    /// </summary>
    public class ColumnFilterDto
    {
        /// <summary>
        /// Column identifier
        /// </summary>
        public string ColumnId { get; set; }
        
        /// <summary>
        /// Filter options
        /// </summary>
        public string FilterOption { get; set; }
        
        /// <summary>
        /// Filter value
        /// </summary>
        public object Filter { get; set; } // string, number, date, date[], number[]
    }
}
