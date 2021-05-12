using System;

namespace Shesha.Web.FormsDesigner.Dtos
{
    /// <summary>
    /// Base class of the form component DTO
    /// </summary>
    public class FormComponentBaseDto
    {
        /// <summary>
        /// Component Id
        /// </summary>
        public Guid? Id { get; set; }

        /// <summary>
        /// Id of the parent component
        /// </summary>
        public Guid? ParentId { get; set; }

        /// <summary>
        /// Component name
        /// </summary>
        public string Name { get; set; }
    }
}
