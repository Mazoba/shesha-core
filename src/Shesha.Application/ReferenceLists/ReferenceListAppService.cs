using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;
using Shesha.Domain;
using Shesha.Services;
using Shesha.Services.ReferenceLists.Dto;
using Shesha.Web.DataTable;

namespace Shesha.ReferenceLists
{
    public class ReferenceListAppService: AsyncCrudAppService<ReferenceList, ReferenceListDto, Guid>
    {
        private readonly ReferenceListHelper _refListHelper;

        public ReferenceListAppService(IRepository<ReferenceList, Guid> repository, ReferenceListHelper refListHelper) : base(repository)
        {
            _refListHelper = refListHelper;
        }
        
        /// <summary>
        /// Get ReferenceList Items
        /// </summary>
        [HttpGet]
        public async Task<List<ReferenceListItemDto>> GetItemsAsync(string @namespace, string name)
        {
            return await _refListHelper.GetItemsAsync(@namespace, name);
        }

        /// <summary>
        /// Clear reference list cache
        /// </summary>
        [HttpPost]
        [Route("/api/services/app/[controller]/ClearCache")]
        public async Task ClearCacheFullAsync() 
        {
            await _refListHelper.ClearCacheAsync();
        }

        [HttpPost]
        [Route("/api/services/app/[controller]/ClearCache/{namespace}/{name}")]
        public async Task ClearCacheAsync(string @namespace, string name)
        {
            await _refListHelper.ClearCacheAsync(@namespace, name);
        }
        
        /// <summary>
        /// Mobile Devices index table
        /// </summary>
        /// <returns></returns>
        public static DataTableConfig IndexTable()
        {
            var table = new DataTableConfig<ReferenceList, Guid>("ReferenceLists_Index");

            table.AddProperty(e => e.Name, c => c.SortAscending());
            table.AddProperty(e => e.Namespace);
            table.AddProperty(e => e.Description);
            table.AddProperty(e => e.HardLinkToApplication);
            table.AddProperty(e => e.CreationTime, c => c.Caption("Created On"));
            
            return table;
        }
    }
}