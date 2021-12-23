using Abp.Application.Services.Dto;
using System;
using System.Collections.Generic;
using System.Text;

namespace Shesha.DynamicEntities.Dtos
{
    /// <summary>
    /// Model configuration DTO
    /// </summary>
    public class ModelConfigurationDto: EntityDto<Guid>
    {
        public string ClassName { get; set; }
        public string Namespace { get; set; }
        public List<EntityPropertyDto> Properties { get; set; }
    }
}
