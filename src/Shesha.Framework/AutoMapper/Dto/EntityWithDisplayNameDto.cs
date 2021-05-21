using Abp.Application.Services.Dto;

namespace Shesha.AutoMapper.Dto
{
    /// <summary>
    /// Generic entity Dto with display text
    /// </summary>
    /// <typeparam name="TPrimaryKey"></typeparam>
    public class EntityWithDisplayNameDto<TPrimaryKey> : EntityDto<TPrimaryKey>
    {
        /// <summary>
        /// Parameterless constructor
        /// </summary>
        public EntityWithDisplayNameDto()
        {

        }

        /// <summary>
        /// Constructor
        /// </summary>
        public EntityWithDisplayNameDto(TPrimaryKey id, string displayText)
        {
            Id = id;
            DisplayText = displayText;
        }

        /// <summary>
        /// Entity display name
        /// </summary>
        public string DisplayText { get; set; }
    }
}
