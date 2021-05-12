using System;
using Abp.Application.Services.Dto;

namespace Shesha.CheckLists.Dtos
{
    /// <summary>
    /// CheckList DTO
    /// </summary>
    public class CheckListDto: EntityDto<Guid>
    {
        /// <summary>
        /// Name of the check list
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; }
    }
}
