using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;
using NHibernate.Linq;
using Shesha.CheckLists.Dtos;
using Shesha.CheckLists.Models;
using Shesha.Domain;

namespace Shesha.CheckLists
{
    /// <summary>
    /// 
    /// </summary>
    public class CheckListItemAppService : SheshaCrudServiceBase<CheckListItem, CheckListItemDto, Guid, PagedAndSortedResultRequestDto, CheckListItemDto, CheckListItemDto>, ICheckListItemAppService
    {
        private readonly IRepository<CheckListTreeItem, Guid> _checkListTreeItemRepository;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="checkListTreeItemRepository"></param>
        public CheckListItemAppService(IRepository<CheckListItem, Guid> repository, IRepository<CheckListTreeItem, Guid> checkListTreeItemRepository) : base(repository)
        {
            _checkListTreeItemRepository = checkListTreeItemRepository;
        }

        /// <summary>
        /// Returns child areas of the specified parent
        /// </summary>
        [HttpPost]
        public async Task<CheckListTreeItemDto> GetTreeItem(EntityDto<Guid> input)
        {
            var item = await _checkListTreeItemRepository.GetAsync(input.Id);
            return ObjectMapper.Map<CheckListTreeItemDto>(item);
        }

        /// <summary>
        /// Returns child items of the specified parent
        /// </summary>
        [HttpPost]
        public async Task<List<CheckListTreeItemDto>> GetChildTreeItems(GetChildCheckListItemsInput input)
        {
            var items = await _checkListTreeItemRepository.GetAll().Where(i => i.CheckListId == input.CheckListId && i.ParentId == input.ParentId)
                .OrderBy(e => e.OrderIndex)
                .ThenBy(e => e.CreationTime)
                .ToListAsync();
            return items.Select(i => ObjectMapper.Map<CheckListTreeItemDto>(i)).ToList();
        }

        /// <summary>
        /// Moves Area to a new parent
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task UpdateChildItemsAsync(UpdateChildItemsInput input)
        {
            var parent = input.ParentId.HasValue
                ? await Repository.GetAsync(input.ParentId.Value)
                : null;
            var items = await Repository.GetAll().Where(i => i.CheckList.Id == input.CheckListId && input.ChildIds.Contains(i.Id)).ToListAsync();

            var orderIndex = 1;
            foreach (var childId in input.ChildIds)
            {
                var item = items.FirstOrDefault(i => i.Id == childId);
                if (item == null)
                    throw new Exception($"Check list item with id = '{childId}' not found");
                item.OrderIndex = orderIndex++;
                item.Parent = parent;
                await Repository.UpdateAsync(item);
            }

            await CurrentUnitOfWork.SaveChangesAsync();
        }

        /// inheritedDoc
        public override async Task DeleteAsync(EntityDto<Guid> input)
        {
            CheckDeletePermission();

            // delete all child items
            var item = await Repository.GetAsync(input.Id);
            await DeleteChildItems(item);

            await Repository.DeleteAsync(input.Id);

            await CurrentUnitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// Deletes all child areas recursively
        /// </summary>
        private async Task DeleteChildItems(CheckListItem item)
        {
            var childItems = await Repository.GetAll().Where(a => a.Parent == item).ToListAsync();
            foreach (var child in childItems)
            {
                await DeleteChildItems(child);
                await Repository.DeleteAsync(child);
            }
            await CurrentUnitOfWork.SaveChangesAsync();
        }
    }
}
