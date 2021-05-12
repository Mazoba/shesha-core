using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Abp.Domain.Entities.Auditing;
using JetBrains.Annotations;
using Shesha.Authorization.Users;
using Shesha.Domain.Attributes;
using Shesha.Domain.Enums;

namespace Shesha.Domain
{
    /// <summary>
    /// A many-to-many relation because of each filter is normally applicable to one or more containers (such as report or a data table)
    /// </summary>
    [Entity(TypeShortAlias = "Shesha.Framework.StoredFilterContainer", FriendlyName = "Filter per container object relation")]
    public class StoredFilterContainer : FullPowerChildEntity
    {
        /// <summary>
        /// Filter link
        /// </summary>
        [NotNull]
        public virtual StoredFilter Filter { get; set; }
        
        /// <summary>
        /// When true, this filter is hidden from all users independent of Visibility settings
        /// </summary>
        public virtual bool IsHidden { get; set; }

        /// <summary>
        /// True for default filter of a container i.e. filter(s) that get immediately applied (selected in dropdown) when the data table or report is loaded. For non-exclusive filters, more than one can be selected as default
        /// </summary>
        public virtual bool IsDefaultFilter { get; set; }
    }
}
