﻿namespace Shesha.DynamicEntities
{
    /// <summary>
    /// Dynamic mapping settings
    /// </summary>
    public interface IDynamicMappingSettings
    {
        /// <summary>
        /// If true, indicates that entity references should be mapped as DTOs (id and display name) instead of raw values (id)
        /// </summary>
        public bool UseDtoForEntityReferences { get; }
    }
}
