using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Linq.Extensions;
using Abp.ObjectMapping;
using Abp.Runtime.Caching;
using ConcurrentCollections;
using NHibernate.Linq;
using Shesha.Authorization;
using Shesha.Domain;
using Shesha.Permissions.Enum;


namespace Shesha.Permissions
{
    public class ProtectedObjectManager : IProtectedObjectManager, ITransientDependency
    {
        private IRepository<ProtectedObject, Guid> _protectedObjectRepository;
        private IUnitOfWorkManager _unitOfWorkManager;
        private ICacheManager _cacheManager;
        private IObjectMapper _objectMapper;

        public ProtectedObjectManager(
            IRepository<ProtectedObject, Guid> protectedObjectRepository,
            IUnitOfWorkManager unitOfWorkManager,
            ICacheManager cacheManager,
            IObjectMapper objectMapper
        )
        {
            _protectedObjectRepository = protectedObjectRepository;
            _unitOfWorkManager = unitOfWorkManager;
            _cacheManager = cacheManager;
            _objectMapper = objectMapper;
        }

        public virtual string GetCategoryByType(Type type)
        {
            var providers = IocManager.Instance.ResolveAll<IProtectedObjectProvider>();
            foreach (var protectedObjectProvider in providers)
            {
                var category = protectedObjectProvider.GetCategoryByType(type);
                if (!string.IsNullOrEmpty(category))
                    return category;
            }

            return null;
        }

        [UnitOfWork]
        public virtual async Task<List<ProtectedObjectDto>> GetAllFlatAsync(string category = null, bool showHidden = false)
        {
            return (await _protectedObjectRepository.GetAll()
                .WhereIf(!string.IsNullOrEmpty(category?.Trim()), x => x.Category == category)
                .WhereIf(!showHidden, x => !x.Hidden)
                .ToListAsync())
                .Select(x => _objectMapper.Map<ProtectedObjectDto>(x))
                .ToList();
        }

        [UnitOfWork]
        public virtual async Task<List<ProtectedObjectDto>> GetAllTreeAsync(string category = null, bool showHidden = false)
        {
            return (await _protectedObjectRepository.GetAll()
                .WhereIf(!string.IsNullOrEmpty(category?.Trim()), x => x.Category == category)
                .WhereIf(!showHidden, x => !x.Hidden)
                .Where(x => x.Parent == null || x.Parent == "")
                .ToListAsync())
                .Select(x => GetObjectWithChild(x, showHidden))
                .ToList();
        }

        [UnitOfWork]
        public virtual async Task<ProtectedObjectDto> GetObjectWithChild(string objectName, bool showHidden = false)
        {
            var obj = await _protectedObjectRepository.GetAll()
                .WhereIf(!showHidden, x => !x.Hidden)
                .FirstOrDefaultAsync(x => x.Parent == null || x.Parent == "");
            return GetObjectWithChild(obj, showHidden);
        }

        private ProtectedObjectDto GetObjectWithChild(ProtectedObject obj, bool showHidden = false)
        {
            var dto = _objectMapper.Map<ProtectedObjectDto>(obj);
            var child = _protectedObjectRepository.GetAll()
                .WhereIf(!showHidden, x => !x.Hidden)
                .Where(x => x.Parent == obj.Object)
                .ToList();
            foreach (var protectedObject in child)
            {
                dto.Child.Add(GetObjectWithChild(protectedObject, showHidden));
            }
            return dto;
        }

        public virtual async Task<ProtectedObjectDto> GetAsync(string objectName, bool useInherited = true,
            UseDependencyType useDependency = UseDependencyType.Before, bool useHidden = false)
        {
            var obj = await _cacheManager.GetProtectedObjectCache().GetOrDefaultAsync(objectName);

            if (obj == null)
            {
                using var unitOfWork = _unitOfWorkManager.Begin();
                var dbObj = await _protectedObjectRepository.GetAll().FirstOrDefaultAsync(x => x.Object == objectName);
                if (dbObj != null)
                {
                    obj = _objectMapper.Map<ProtectedObjectDto>(dbObj);
                    _cacheManager.GetProtectedObjectCache().Set(objectName, obj);
                }
                unitOfWork.Complete();
            }

            // Check hidden, dependency and inherited
            if (obj != null)
            {
                // skip hidden
                if (!useHidden && obj.Hidden)
                    return null;

                // get dependency
                var dep = !string.IsNullOrEmpty(obj.Dependency)
                    ? await GetAsync(obj.Dependency, true, useDependency, useHidden)
                    : null;

                // check dependency before
                if (useDependency == UseDependencyType.Before && dep != null && !dep.Inherited && dep.Permissions.Any())
                    return dep;

                // if current object is inherited
                if (useInherited && obj.Inherited && !string.IsNullOrEmpty(obj.Parent))
                {
                    var parent = await GetAsync(obj.Parent, true, UseDependencyType.NotUse, useHidden);

                    // check parent
                    if (parent != null && !parent.Inherited && parent.Permissions.Any())
                        return parent;

                    // check dependency after
                    if (useDependency == UseDependencyType.After && dep != null && !dep.Inherited && dep.Permissions.Any())
                        return dep;
                }
            }

            return obj;
        }

        [UnitOfWork]
        public virtual async Task<ProtectedObjectDto> SetAsync(ProtectedObjectDto protectedObject)
        {
            // ToDo: AS - check permission names exist
            var obj = await _protectedObjectRepository.GetAll().FirstOrDefaultAsync(x =>
                          x.Object == protectedObject.Object
                          && x.Category == protectedObject.Category) 
                      ??
                      new ProtectedObject()
                      {
                          Object = protectedObject.Object,
                          Category = protectedObject.Category,
                          Parent = protectedObject.Parent,
                      };

            obj.Description = protectedObject.Description;
            obj.Permissions = string.Join(",", protectedObject.Permissions ?? new ConcurrentHashSet<string>());
            obj.Inherited = protectedObject.Inherited;
            obj.Hidden = protectedObject.Hidden;

            await _protectedObjectRepository.InsertOrUpdateAsync(obj);

            await _cacheManager.GetProtectedObjectCache().SetAsync(protectedObject.Object, protectedObject);

            return protectedObject;
        }

        [UnitOfWork]
        public virtual async Task<ProtectedObjectDto> SetPermissionsAsync(string objectName, bool inherited, List<string> permissions)
        {
            // ToDo: AS - check permission names exist
            var obj = await _protectedObjectRepository.GetAll().FirstOrDefaultAsync(x => x.Object == objectName);

            if (obj == null) return null;

            obj.Permissions = string.Join(",", permissions ?? new List<string>());
            obj.Inherited = inherited;
            await _protectedObjectRepository.InsertOrUpdateAsync(obj);

            var dto = _objectMapper.Map<ProtectedObjectDto>(obj);
            await _cacheManager.GetProtectedObjectCache().SetAsync(objectName, dto);

            return dto;
        }

        public virtual async Task ClearCacheAsync()
        {
            await _cacheManager.GetProtectedObjectCache().ClearAsync();
        }

    }

}