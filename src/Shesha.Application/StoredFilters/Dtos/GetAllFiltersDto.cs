using Shesha.AutoMapper.Dto;

namespace Shesha.StoredFilters.Dto
{
    /// <summary>
    /// Get filters list: either all or by container
    /// </summary>
    public class GetAllFiltersDto : ChildEntityGetListInputDto
    {
        /// <summary>
        /// Data table ID or a report ID. Empty string for full list of filters
        /// </summary>
        public override string OwnerId { get => base.OwnerId; set => base.OwnerId = value; }

        /// <summary>
        /// Empty string for data tables or type short alias for report or other container
        /// </summary>
        public override string OwnerType { get => base.OwnerType; set => base.OwnerType = value; }
    }
}
