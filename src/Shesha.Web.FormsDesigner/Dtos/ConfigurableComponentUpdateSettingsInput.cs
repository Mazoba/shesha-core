using System;
using Abp.Application.Services.Dto;

namespace Shesha.Web.FormsDesigner.Dtos
{
    /// <summary>
    /// Update component settings input
    /// </summary>
    public class ConfigurableComponentUpdateSettingsInput : EntityDto<Guid>
    {
        /// <summary>
        /// Settings in JSON format
        /// </summary>
        public string Settings { get; set; }
    }
}
