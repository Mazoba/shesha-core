using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Reflection;
using NHibernate.Linq;
using Shesha.Domain;
using Shesha.Domain.Attributes;
using Shesha.Reflection;
using Shesha.Utilities;

namespace Shesha.Bootstrappers
{
    public class ReferenceListBootstrapper: IBootstrapper, ITransientDependency
    {
        private readonly ITypeFinder _typeFinder;
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly IRepository<ReferenceList, Guid> _listRepo;
        private readonly IRepository<ReferenceListItem, Guid> _listItemRepo;

        public ReferenceListBootstrapper(ITypeFinder typeFinder, IUnitOfWorkManager unitOfWorkManager, IRepository<ReferenceList, Guid> listRepo, IRepository<ReferenceListItem, Guid> listItemRepo)
        {
            _typeFinder = typeFinder;
            _unitOfWorkManager = unitOfWorkManager;
            _listRepo = listRepo;
            _listItemRepo = listItemRepo;
        }

        public async Task Process()
        {
            using (_unitOfWorkManager.Current.DisableFilter(AbpDataFilters.SoftDelete))
            {
                await DoProcess();
            }
        }

        private async Task DoProcess()
        {
            var lists = _typeFinder
                .Find(type => type != null && type.IsPublic && type.IsEnum && type.HasAttribute<ReferenceListAttribute>())
                .Select(e => new
                {
                    Enum = e,
                    Attribute = e.GetAttribute<ReferenceListAttribute>()
                })
                .ToList();

            if (!lists.Any())
                return;

            foreach (var list in lists)
            {

                try
                {
                    var listInCode = new List<ListItemInfo>();
                    var values = Enum.GetValues(list.Enum);
                    foreach (var value in values)
                    {
                        var intValue =  Convert.ToInt64(value);
                        var internalName = Enum.GetName(list.Enum, intValue);
                        var memberInfo = list.Enum.GetMember(internalName).FirstOrDefault();

                        var displayAttribute = memberInfo != null
                            ? memberInfo.GetAttribute<DisplayAttribute>()
                            : null;

                        var descriptionAttribute = memberInfo != null
                            ? memberInfo.GetAttribute<DescriptionAttribute>()
                            : null;

                        if (displayAttribute != null && displayAttribute.GetAutoGenerateField() == false)
                            continue;

                        listInCode.Add(new ListItemInfo
                        {
                            Name = displayAttribute != null
                                ? displayAttribute.Name
                                : descriptionAttribute != null
                                    ? descriptionAttribute.Description
                                    : internalName.ToFriendlyName(),
                            Description = descriptionAttribute?.Description,
                            Value = intValue,
                            OrderIndex = displayAttribute?.GetOrder() ?? intValue
                        });
                    }
                    
                    var listInDb = await _listRepo.GetAll()
                        .FirstOrDefaultAsync(l =>
                                    l.Name == list.Attribute.ReferenceListName &&
                                    l.Namespace == list.Attribute.Namespace);
                    if (listInDb == null)
                    {
                        listInDb = new ReferenceList()
                        {
                            Name = list.Attribute.ReferenceListName,
                            Namespace = list.Attribute.Namespace
                        };
                        await _listRepo.InsertAsync(listInDb);
                    }

                    var itemsInDb = await _listItemRepo.GetAll()
                        .Where(i => i.ReferenceList == listInDb)
                        .ToListAsync();

                    var toAdd = listInCode.Where(i => !itemsInDb.Any(iv => iv.ItemValue == i.Value)).ToList();

                    foreach (var item in toAdd)
                    {
                        var newItem = new ReferenceListItem()
                        {
                            ItemValue = item.Value,
                            Item = item.Name,
                            Description = item.Description,
                            OrderIndex = item.OrderIndex,
                            ReferenceList = listInDb
                        };
                        newItem.SetHardLinkToApplication(true);

                        await _listItemRepo.InsertOrUpdateAsync(newItem);
                    }

                    var toInactivate = itemsInDb.Where(ldb => ldb.HardLinkToApplication && !listInCode.Any(i => i.Value == ldb.ItemValue)).ToList();
                    foreach (var item in toInactivate)
                    {
                        await _listItemRepo.DeleteAsync(item);
                    }

                    var toUpdate = itemsInDb.Select(idb => new
                    {
                        ItemInDB = idb,
                        UpdatedItemInCode = listInCode.FirstOrDefault(i => i.Value == idb.ItemValue && (i.Name != idb.Item || !idb.HardLinkToApplication))
                    })
                        .Where(i => i.UpdatedItemInCode != null)
                        .ToList();
                    foreach (var item in toUpdate)
                    {
                        item.ItemInDB.Item = item.UpdatedItemInCode.Name;
                        item.ItemInDB.SetHardLinkToApplication(true);
                        await _listItemRepo.InsertOrUpdateAsync(item.ItemInDB);
                    }

                    await _listRepo.InsertOrUpdateAsync(listInDb);

                    await _unitOfWorkManager.Current.SaveChangesAsync();
                }
                catch (Exception e)
                {
                    throw new Exception($"An error occured during bootstrapping of the referenceList {list.Attribute.ReferenceListName}.{list.Attribute.Namespace}", e);
                }
            }
        }

        private class ListItemInfo
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public Int64 Value { get; set; }
            public Int64 OrderIndex { get; set; }
        }
    }
}