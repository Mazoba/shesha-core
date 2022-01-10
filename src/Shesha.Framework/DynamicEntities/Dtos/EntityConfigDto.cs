using Abp.Application.Services.Dto;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Shesha.DynamicEntities.Dtos
{
    /// <summary>
    /// Entity config DTO
    /// </summary>
    public class EntityConfigDto: EntityDto<Guid>
    {
        [StringLength(255)]
        public string FriendlyName { get; set; }
        [StringLength(100)]
        public string TypeShortAlias { get; set; }
        [StringLength(255)]
        public string TableName { get; set; }
        [StringLength(500)]
        public string ClassName { get; set; }
        [StringLength(500)]
        public string Namespace { get; set; }
        [StringLength(255)]
        public string DiscriminatorValue { get; set; }
    }
}
