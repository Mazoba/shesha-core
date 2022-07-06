using Abp.Application.Services.Dto;

namespace Shesha.Application.Services.Dto
{
    /// <summary>
    /// Input that is used to fetch entity data dynamically
    /// </summary>
    /// <typeparam name="TId"></typeparam>
    public class GetDynamicEntityInput<TId> : EntityDto<TId>
    {
        /// <summary>
        /// List of properties to fetch in GraphQL-like syntax. Supports nested properties 
        /// </summary>
        public string Properties { get; set; }
    }
}
