using System;
using Abp.Application.Services;
using Abp.Domain.Repositories;
using Shesha.Domain;
using Shesha.Services;
using Shesha.Services.ReferenceLists.Dto;
using Shesha.Web.DataTable;
using Shesha.Web.DataTable;

namespace Shesha.ReferenceLists
{
    public class ReferenceListItemAppService : AsyncCrudAppService<ReferenceListItem, ReferenceListItemDto, Guid>
    {
        private readonly ReferenceListHelper _refListHelper;

        public ReferenceListItemAppService(IRepository<ReferenceListItem, Guid> repository, ReferenceListHelper refListHelper) : base(repository)
        {
            _refListHelper = refListHelper;
        }

        /// <summary>
        /// Mobile Devices index table
        /// </summary>
        /// <returns></returns>
        public static DataTableConfig IndexTable()
        {
            var table = ChildDataTableConfig<ReferenceList, ReferenceListItem, Guid>.OneToMany("ReferenceList_Items", i => i.ReferenceList);

            table.AddProperty(e => e.OrderIndex, c => c.SortAscending());
            table.AddProperty(e => e.Item);
            table.AddProperty(e => e.ItemValue);
            table.AddProperty(e => e.Description);
            table.AddProperty(e => e.HardLinkToApplication);
            table.AddProperty(e => e.CreationTime, c => c.Caption("Created On"));
            
            return table;
        }
    }
}