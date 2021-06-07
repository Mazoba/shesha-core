using System;
using Abp.Application.Services.Dto;

namespace Shesha.Web.FormsDesigner.Dtos
{
    /// <summary>
    /// Configurable Component DTO
    /// </summary>
    public class ConfigurableComponentDto : EntityDto<Guid>
    {
        /// <summary>
        /// Form path/id is used to identify a form
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Form name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Settings in JSON format
        /// </summary>
        public string Settings { get; set; }

        /// <summary>
        /// Type of the form model
        /// </summary>
        public string ModelType { get; set; }
    }
}
