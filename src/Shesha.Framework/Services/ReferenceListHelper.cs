using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Events.Bus.Entities;
using Abp.Events.Bus.Handlers;
using Abp.ObjectMapping;
using Abp.Runtime.Caching;
using NHibernate.Linq;
using Shesha.Domain;
using Shesha.Services.ReferenceLists.Dto;

namespace Shesha.Services
{
    public class ReferenceListHelper: IEventHandler<EntityChangedEventData<ReferenceListItem>>, IReferenceListHelper, ITransientDependency
    {
        private const string ListItemsCacheName = "ReferenceListCache";

        private readonly IRepository<ReferenceList, Guid> _listRepository;
        private readonly IRepository<ReferenceListItem, Guid> _itemsRepository;
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly ICacheManager _cacheManager;

        /// <summary>
        /// Reference to the object to object mapper.
        /// </summary>
        public IObjectMapper ObjectMapper { get; set; }

        /// <summary>
        /// Cache of the ReferenceListItems
        /// </summary>
        protected ITypedCache<string, List<ReferenceListItemDto>> ListItemsCache => _cacheManager.GetCache<string, List<ReferenceListItemDto>>(ListItemsCacheName);


        public ReferenceListHelper(IRepository<ReferenceList, Guid> listRepository, IRepository<ReferenceListItem, Guid> itemsRepository, IUnitOfWorkManager unitOfWorkManager, ICacheManager cacheManager)
        {
            _listRepository = listRepository;
            _itemsRepository = itemsRepository;
            _unitOfWorkManager = unitOfWorkManager;
            _cacheManager = cacheManager;
        }

        /// <summary>
        /// Returns display name of the <see cref="ReferenceListItem"/> in the specified list
        /// </summary>
        /// <param name="refListNamespace">Namespace of the <see cref="ReferenceList"/></param>
        /// <param name="refListName">Name of the <see cref="ReferenceList"/></param>
        /// <param name="value">Value of the <see cref="ReferenceListItem"/></param>
        /// <returns></returns>
        public string GetItemDisplayText(string refListNamespace, string refListName, Int64? value)
        {
            if (string.IsNullOrWhiteSpace(refListNamespace))
                throw new Exception($"{nameof(refListNamespace)} must not be null");
            if (string.IsNullOrWhiteSpace(refListName))
                throw new Exception($"{nameof(refListName)} must not be null");

            if (value == null)
                return null;
            
            // make sure that we have active session
            using (_unitOfWorkManager.Current == null ? _unitOfWorkManager.Begin() : null)
            {
                var items = GetItems(refListNamespace, refListName);
                return items.FirstOrDefault(i => i.ItemValue == value)?.Item;
            }
        }

        /// inheritedDoc
        public List<ReferenceListItemDto> DecomposeMultiValueIntoItems(string refListNamespace, string refListName, Int64? value)
        {
            if (value == null)
                return new List<ReferenceListItemDto>();

            var values = Extensions.EntityExtensions.DecomposeIntoBitFlagComponents(value);
            var selectedItems = GetItems(refListNamespace, refListName)
                .Where(i => values.Contains(i.ItemValue))
                .ToList();
            
            return selectedItems;
        }

        /// <summary>
        /// Returns <see cref="ReferenceList"/> by name and namespace
        /// </summary>
        /// <param name="nameSpace">Namespace of the <see cref="ReferenceList"/></param>
        /// <param name="name">Name of the <see cref="ReferenceList"/></param>
        /// <returns></returns>
        public ReferenceList GetReferenceList(string nameSpace, string name)
        {
            return _listRepository.GetAll().FirstOrDefault(l => l.Namespace == nameSpace && l.Name == name);
        }

        /// <summary>
        /// Return reference list items
        /// </summary>
        public async Task<List<ReferenceListItemDto>> GetItemsAsync(string @namespace, string name)
        {
            if (string.IsNullOrWhiteSpace(@namespace) || string.IsNullOrWhiteSpace(name))
                return new List<ReferenceListItemDto>();

            var cacheKey = $"{@namespace}.{name}";
            var cachedList = await ListItemsCache.GetOrDefaultAsync(cacheKey);
            if (cachedList != null)
                return cachedList;

            var items = await _itemsRepository.GetAll()
                .Where(e => e.ReferenceList.Namespace == @namespace && e.ReferenceList.Name == name)
                .OrderBy(e => e.OrderIndex).ThenBy(e => e.Item)
                .ToListAsync();

            var itemDtos = items.Select(e => ObjectMapper.Map<ReferenceListItemDto>(e)).ToList();

            await ListItemsCache.SetAsync(cacheKey,
                itemDtos,
                absoluteExpireTime: TimeSpan.FromMinutes(60));

            return itemDtos;
        }

        /// <summary>
        /// Return reference list items
        /// </summary>
        public List<ReferenceListItemDto> GetItems(string @namespace, string name)
        {
            if (string.IsNullOrWhiteSpace(@namespace) || string.IsNullOrWhiteSpace(name))
                return new List<ReferenceListItemDto>();

            var cacheKey = $"{@namespace}.{name}";
            var cachedList = ListItemsCache.GetOrDefault(cacheKey);
            if (cachedList != null)
                return cachedList;

            var items = _itemsRepository.GetAll()
                .Where(e => e.ReferenceList.Namespace == @namespace && e.ReferenceList.Name == name)
                .OrderBy(e => e.OrderIndex).ThenBy(e => e.Item)
                .ToList();

            var itemDtos = items.Select(e => ObjectMapper.Map<ReferenceListItemDto>(e)).ToList();

            ListItemsCache.Set(cacheKey,
                itemDtos,
                absoluteExpireTime: TimeSpan.FromMinutes(60));

            return itemDtos;
        }

        private string GetCacheKey(string @namespace, string name)
        {
            return $"{@namespace}.{name}";
        }

        private string GetCacheKey(ReferenceList refList)
        {
            return GetCacheKey(refList.Namespace, refList.Name);
        }

        public void HandleEvent(EntityChangedEventData<ReferenceListItem> eventData)
        {
            var refList = eventData.Entity?.ReferenceList;

            if (refList == null)
                return;

            ListItemsCache.Remove(GetCacheKey(refList));
        }

        /// <summary>
        /// Clear reference list cache
        /// </summary>
        public async Task ClearCacheAsync()
        {
            await ListItemsCache.ClearAsync();
        }

        /// <summary>
        /// Clear reference list cache
        /// </summary>
        public async Task ClearCacheAsync(string @namespace, string name)
        {
            await ListItemsCache.RemoveAsync(GetCacheKey(@namespace, name));
        }
    }
}
