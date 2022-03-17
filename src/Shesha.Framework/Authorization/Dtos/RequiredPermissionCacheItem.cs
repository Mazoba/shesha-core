using System.Collections.Generic;
using ConcurrentCollections;

namespace Shesha.Authorization.Dtos
{
    public class RequiredPermissionCacheItem
    {
        public const string CacheStoreName = "RequiredPermissionCache";

        /// <summary>
        /// Changed from HashSet to ConcurrentHashSet because of the `Operations that change non-concurrent collections must have exclusive access` exception in `HashSet`1.Contains`
        /// </summary>
        public ConcurrentHashSet<string> RequiredPermissions { get; set; }

        public RequiredPermissionCacheItem()
        {
            RequiredPermissions = new ConcurrentHashSet<string>();
        }

        /*public RequiredPermissionCacheItem(long userId)
            : this()
        {
            UserId = userId;
        }*/
    }
}
