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
    public class PermissionedObjectManager : IPermissionedObjectManager, ITransientDependency
    {
        private IRepository<PermissionedObject, Guid> _permissionedObjectRepository;
        private IUnitOfWorkManager _unitOfWorkManager;
        private ICacheManager _cacheManager;
        private IObjectMapper _objectMapper;

        public PermissionedObjectManager(
            IRepository<PermissionedObject, Guid> permissionedObjectRepository,
            IUnitOfWorkManager unitOfWorkManager,
            ICacheManager cacheManager,
            IObjectMapper objectMapper
        )
        {
            _permissionedObjectRepository = permissionedObjectRepository;
            _unitOfWorkManager = unitOfWorkManager;
            _cacheManager = cacheManager;
            _objectMapper = objectMapper;
        }

        public virtual string GetObjectType(Type type)
        {
            var providers = IocManager.Instance.ResolveAll<IPermissionedObjectProvider>();
            foreach (var permissionedObjectProvider in providers)
            {
                var objType = permissionedObjectProvider.GetObjectType(type);
                if (!string.IsNullOrEmpty(objType))
                    return objType;
            }

            return null;
        }

        [UnitOfWork]
        public virtual async Task<List<PermissionedObjectDto>> GetAllFlatAsync(string type = null, bool withNested = true, bool withHidden = false)
        {
            var root = (await _permissionedObjectRepository.GetAll()
                .WhereIf(!string.IsNullOrEmpty(type?.Trim()), x => x.Type == type)
                .WhereIf(!withHidden, x => !x.Hidden)
                .ToListAsync())
                .Select(x => _objectMapper.Map<PermissionedObjectDto>(x))
                .OrderBy(x => x.Name)
                .ToList();

            if (withNested && !string.IsNullOrEmpty(type?.Trim()))
            {
                var nested = (await _permissionedObjectRepository.GetAll()
                        .Where(x => x.Type.Contains($"{type}."))
                        .WhereIf(!withHidden, x => !x.Hidden)
                        .ToListAsync())
                    .Select(x => _objectMapper.Map<PermissionedObjectDto>(x))
                    .OrderBy(x => x.Name)
                    .ToList();
                root.AddRange(nested);
            }

            return root;
        }

        [UnitOfWork]
        public virtual async Task<List<PermissionedObjectDto>> GetAllTreeAsync(string type = null, bool withHidden = false)
        {
            return (await _permissionedObjectRepository.GetAll()
                .WhereIf(!string.IsNullOrEmpty(type?.Trim()), x => x.Type == type)
                .WhereIf(!withHidden, x => !x.Hidden)
                .Where(x => x.Parent == null || x.Parent == "")
                .ToListAsync())
                .OrderBy(x => x.Name)
                .Select(x => GetObjectWithChild(x, withHidden))
                .ToList();
        }

        public virtual async Task<PermissionedObjectDto> GetObjectWithChild(string objectName, bool withHidden = false)
        {
            var obj = await _permissionedObjectRepository.GetAll()
                .WhereIf(!withHidden, x => !x.Hidden)
                .FirstOrDefaultAsync(x => x.Parent == null || x.Parent == "");
            return GetObjectWithChild(obj, withHidden);
        }

        private PermissionedObjectDto GetObjectWithChild(PermissionedObject obj, bool withHidden = false)
        {
            var dto = _objectMapper.Map<PermissionedObjectDto>(obj);
            var child = _permissionedObjectRepository.GetAll()
                .WhereIf(!withHidden, x => !x.Hidden)
                .Where(x => x.Parent == obj.Object)
                .OrderBy(x => x.Name)
                .ToList();
            foreach (var permissionedObject in child)
            {
                dto.Child.Add(GetObjectWithChild(permissionedObject, withHidden));
            }
            return dto;
        }

        public virtual async Task<PermissionedObjectDto> GetAsync(string objectName, bool useInherited = true,
            UseDependencyType useDependency = UseDependencyType.Before, bool useHidden = false)
        {
            var obj = await _cacheManager.GetPermissionedObjectCache().GetOrDefaultAsync(objectName);

            if (obj == null)
            {
                using var unitOfWork = _unitOfWorkManager.Begin();
                var dbObj = await _permissionedObjectRepository.GetAll().FirstOrDefaultAsync(x => x.Object == objectName);
                if (dbObj != null)
                {
                    obj = _objectMapper.Map<PermissionedObjectDto>(dbObj);
                    _cacheManager.GetPermissionedObjectCache().Set(objectName, obj);
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
        public virtual async Task<PermissionedObjectDto> SetAsync(PermissionedObjectDto permissionedObject)
        {
            // ToDo: AS - check if permission names exist
            var obj = await _permissionedObjectRepository.GetAll().FirstOrDefaultAsync(x =>
                          x.Object == permissionedObject.Object
                          && x.Type == permissionedObject.Type
                          && x.Module == permissionedObject.Module) 
                      ??
                      new PermissionedObject()
                      {
                          Object = permissionedObject.Object,
                          Type = permissionedObject.Type,
                          Module = permissionedObject.Module,
                          Parent = permissionedObject.Parent,
                          Name = permissionedObject.Name
                      };

            obj.Category = permissionedObject.Category;
            obj.Description = permissionedObject.Description;
            obj.Permissions = string.Join(",", permissionedObject.Permissions ?? new ConcurrentHashSet<string>());
            obj.Inherited = permissionedObject.Inherited;
            obj.Hidden = permissionedObject.Hidden;

            await _permissionedObjectRepository.InsertOrUpdateAsync(obj);

            await _cacheManager.GetPermissionedObjectCache().SetAsync(permissionedObject.Object, permissionedObject);

            return permissionedObject;
        }

        [UnitOfWork]
        public virtual async Task<PermissionedObjectDto> SetPermissionsAsync(string objectName, bool inherited, List<string> permissions)
        {
            // ToDo: AS - check permission names exist
            var obj = await _permissionedObjectRepository.GetAll().FirstOrDefaultAsync(x => x.Object == objectName);

            if (obj == null) return null;

            obj.Permissions = string.Join(",", permissions ?? new List<string>());
            obj.Inherited = inherited;
            await _permissionedObjectRepository.InsertOrUpdateAsync(obj);

            var dto = _objectMapper.Map<PermissionedObjectDto>(obj);
            await _cacheManager.GetPermissionedObjectCache().SetAsync(objectName, dto);

            return dto;
        }

        public virtual async Task ClearCacheAsync()
        {
            await _cacheManager.GetPermissionedObjectCache().ClearAsync();
        }

    }

}