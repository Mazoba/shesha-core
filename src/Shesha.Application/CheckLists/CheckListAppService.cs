using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Domain.Repositories;
using Abp.Runtime.Validation;
using Microsoft.AspNetCore.Mvc;
using NHibernate.Linq;
using Shesha.CheckLists.Dtos;
using Shesha.CheckLists.Models;
using Shesha.Domain;
using Shesha.Domain.Enums;
using Shesha.Utilities;
using Shesha.Web.DataTable;

namespace Shesha.CheckLists
{
    /// <summary>
    /// Check list application service
    /// </summary>
    public class CheckListAppService : SheshaCrudServiceBase<CheckList, CheckListDto, Guid, PagedAndSortedResultRequestDto, CheckListDto, CheckListDto>, ICheckListAppService
    {
        private readonly IRepository<CheckListItemSelection, Guid> _selectionRepository;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="repository"></param>
        public CheckListAppService(IRepository<CheckList, Guid> repository, IRepository<CheckListItemSelection, Guid> selectionRepository) : base(repository)
        {
            _selectionRepository = selectionRepository;
        }

        #region DataTable configurations

        /// <summary>
        /// Index table configuration
        /// </summary>
        /// <returns></returns>
        public static DataTableConfig IndexTable()
        {
            var table = new DataTableConfig<CheckList, Guid>("CheckList_Index");
            table.AddProperty(e => e.Name);
            table.AddProperty(e => e.Description);

            return table;
        }

        #endregion

        /// <summary>
        /// Get user selection
        /// </summary>
        [HttpGet]
        [Route("/checkList/{id}/selection")]
        public async Task<List<CheckListItemSelectionDto>> GetSelectionAsync(GetSelectionInput input)
        {
            var selections = await _selectionRepository.GetAll()
                .Where(i =>
                    i.OwnerType == input.OwnerType && 
                    i.OwnerId == input.OwnerId && 
                    i.CheckListItem.CheckList.Id == input.Id)
                .ToListAsync();

            var lastSelections = selections.GroupBy(i => i.CheckListItem, (checkListItem, items) =>
                    new
                    {
                        CheckListItem = checkListItem,
                        LastSelection = items.OrderByDescending(s => s.CreationTime).FirstOrDefault()
                    })
                .Select(g => g.LastSelection)
                .ToList();

            var result = lastSelections.Select(i => new CheckListItemSelectionDto
            {
                CheckListItemId = i.CheckListItem.Id,
                Comments = i.Comments,
                Selection = (int?)i.Selection

            }).ToList();

            return result;
        }

        /// <summary>
        /// Save user selection
        /// </summary>
        [HttpPost]
        [Route("/checkList/{id}/selection")]
        public async Task SaveSelectionAsync(SaveSelectionInput input)
        {
            var selections = await _selectionRepository.GetAll()
                .Where(i =>
                    i.OwnerType == input.OwnerType &&
                    i.OwnerId == input.OwnerId &&
                    i.CheckListItem.CheckList.Id == input.Id)
                .ToListAsync();

            var duplicates = input.Selection.GroupBy(i => i.CheckListItemId, (id, items) =>
                    new
                    {
                        ItemId = id,
                        Items = items
                    })
                .Where(g => g.Items.Count() > 1)
                .ToList();
            if (duplicates.Any())
                throw new AbpValidationException($"Duplicated check list items posted: {duplicates.Select(g => g.ItemId.ToString()).Delimited(", ")}");

            var checkList = await Repository.GetAsync(input.Id);

            foreach (var postedItem in input.Selection)
            {
                var selection = selections.FirstOrDefault(i => i.CheckListItem.Id == postedItem.CheckListItemId);
                if (selection == null)
                {
                    var checkListItem = checkList.Items.FirstOrDefault(i => i.Id == postedItem.CheckListItemId);
                    if (checkListItem == null)
                        throw new Exception($"CheckList item with Id = `{postedItem.CheckListItemId}` not found");

                    selection = new CheckListItemSelection
                    {
                        CheckListItem = checkListItem,
                        OwnerType = input.OwnerType,
                        OwnerId = input.OwnerId,

                    };
                }

                selection.Selection = (RefListCheckListSelectionType?)postedItem.Selection;
                selection.Comments = postedItem.Comments;

                await _selectionRepository.InsertOrUpdateAsync(selection);
            }
        }

        /// <summary>
        /// Get check list tree
        /// </summary>
        [HttpGet]
        [Route("/checkList/{id}/tree")]
        public async Task<CheckListModel> GetCheckListTreeAsync(Guid id)
        {
            var checkList = await Repository.GetAsync(id);

            var model = new CheckListModel()
            {
                Id = checkList.Id,
                Name = checkList.Name,
                Description = checkList.Description,
            };

            FillTreeLevel(checkList.Items, model.Items, null);

            return model;
        }

        private void FillTreeLevel(IList<CheckListItem> source, List<CheckListItemModel> destination, Guid? parentId)
        {
            var currentLevel = source
                .Where(i => i.Parent?.Id == parentId)
                .OrderBy(i => i.OrderIndex)
                .ThenBy(i => i.CreationTime)
                .ToList();

            foreach (var item in currentLevel)
            {
                var treeItem = new CheckListItemModel()
                {
                    Id = item.Id,
                    ItemType = (int) item.ItemType,
                    Name = item.Name,
                    Description = item.Description,
                    AllowAddComments = item.AllowAddComments,
                    CommentsHeading = item.CommentsHeading,
                    CommentsVisibilityExpression = item.CommentsVisibilityExpression,
                    ChildItems = new List<CheckListItemModel>()
                };
                destination.Add(treeItem);
                
                // add child items recursively
                FillTreeLevel(source, treeItem.ChildItems, treeItem.Id);
            }
        }
    }
}