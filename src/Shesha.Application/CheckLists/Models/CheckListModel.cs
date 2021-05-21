using System;
using System.Collections.Generic;
using Abp.Application.Services.Dto;

namespace Shesha.CheckLists.Models
{
    /// <summary>
    /// Checklist model
    /// </summary>
    public class CheckListModel : EntityDto<Guid>
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public CheckListModel()
        {
            Items = new List<CheckListItemModel>();
        }

        /// <summary>
        /// Name of the check list
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Items of the check list
        /// </summary>
        public List<CheckListItemModel> Items { get; set; }
    }
}
