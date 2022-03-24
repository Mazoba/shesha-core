﻿using Abp.Runtime.Caching;
using Shesha.Authorization.Dtos;
using Shesha.Permissions;

namespace Shesha.Authorization
{
    public static class CacheManagerExtensions
    {
        public static ITypedCache<string, CustomUserPermissionCacheItem> GetCustomUserPermissionCache(this ICacheManager cacheManager)
        {
            return cacheManager.GetCache<string, CustomUserPermissionCacheItem>(CustomUserPermissionCacheItem.CacheStoreName);
        }

        public static ITypedCache<string, ProtectedObjectDto> GetProtectedObjectCache(this ICacheManager cacheManager)
        {
            return cacheManager.GetCache<string, ProtectedObjectDto>(ProtectedObjectDto.CacheStoreName);
        }

    }
}
