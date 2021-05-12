using System;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using Shesha.Domain;

namespace Shesha.Services.ReferenceLists.Dto
{
    /// <summary>
    /// Dto of the <see cref="ReferenceList"/>
    /// </summary>
    [AutoMap(typeof(ReferenceList))]
    public class ReferenceListDto: EntityDto<Guid>
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool HardLinkToApplication { get; set; }
        public string Namespace { get; set; }
        public int? NoSelectionValue { get; set; }
    }
}
