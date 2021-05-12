using Shesha.Domain.Enums;

namespace Shesha.Web.DataTable.Model
{
    /// <summary>
    /// Stored filter DTO
    /// </summary>
    public class SelectedStoredFilterDto
    {
        /// <summary>
        /// Filter Id
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Filter name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Typ eof expression (JsonLogic/hql/sql etc)
        /// </summary>
        public string ExpressionType { get; set; }
        
        /// <summary>
        /// Expression body
        /// </summary>
        public object Expression { get; set; }
    }
}
