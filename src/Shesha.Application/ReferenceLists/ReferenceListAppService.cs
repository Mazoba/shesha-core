using Abp.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;
using Shesha.Domain;
using Shesha.Services;
using Shesha.Services.ReferenceLists.Dto;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shesha.ReferenceLists
{
    public class ReferenceListAppService: SheshaCrudServiceBase<ReferenceList, ReferenceListDto, Guid>
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
    }
}