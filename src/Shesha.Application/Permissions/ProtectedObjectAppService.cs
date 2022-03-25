using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Shesha.Domain;

namespace Shesha.Permissions
{
    [AbpAuthorize()]
    public class ProtectedObjectAppService : SheshaCrudServiceBase<ProtectedObject, ProtectedObjectDto, Guid>, IProtectedObjectAppService
    {
        private readonly ProtectedObjectManager _protectedObjectManager;

        public ProtectedObjectAppService(
            IRepository<ProtectedObject, Guid> repository,
            ProtectedObjectManager protectedObjectManager
            ) : base(repository)
        {
            _protectedObjectManager = protectedObjectManager;
        }
        
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="repository"></param>
        public ProtectedObjectAppService(
            IRepository<ProtectedObject, Guid> repository
            ) : base(repository)
        {
        }

        /// <summary>
        /// Get list of protected objects
        /// </summary>
        /// <param name="category"></param>
        /// <param name="showHidden"></param>
        /// <returns></returns>
        public async Task<List<ProtectedObjectDto>> GetAllFlatAsync(string category, bool showHidden = false)
        {
            return await _protectedObjectManager.GetAllFlatAsync(category, showHidden);
        }

        /// <summary>
        /// Get hierarchical list of protected objects
        /// </summary>
        /// <param name="category"></param>
        /// <param name="showHidden"></param>
        /// <returns></returns>
        public async Task<List<ProtectedObjectDto>> GetAllTreeAsync(string category, bool showHidden = false)
        {
            return await _protectedObjectManager.GetAllTreeAsync(category, showHidden);
        }

        /// <summary>
        /// Get protected object by name
        /// </summary>
        /// <param name="objectName"></param>
        /// <returns></returns>
        public async Task<ProtectedObjectDto> GetByObjectNameAsync(string objectName)
        {
            return await _protectedObjectManager.GetAsync(objectName);
        }

        /// <summary>
        /// Set required permissions for protected object by name
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="inherited"></param>
        /// <param name="permissions"></param>
        /// <returns></returns>
        public async Task<ProtectedObjectDto> SetPermissionsAsync(string objectName, bool inherited, List<string> permissions)
        {
            return await _protectedObjectManager.SetPermissionsAsync(objectName, inherited, permissions);
        }

        /// <summary>
        /// Get protected object for API by Service and Action names
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="actionName"></param>
        /// <returns></returns>
        public async Task<ProtectedObjectDto> GetApiPermissionsAsync(string serviceName, string actionName)
        {
            var action = string.IsNullOrEmpty(actionName) ? "" : "@" + actionName;
            return await _protectedObjectManager.GetAsync($"{serviceName}{action}");
        }

        /// <summary>
        /// Update protected object data
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override async Task<ProtectedObjectDto> UpdateAsync(ProtectedObjectDto input)
        {
            return await _protectedObjectManager.SetAsync(input);
        }

        /// <summary>
        /// Set required permissions for API by Service and Action names
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="actionName"></param>
        /// <param name="inherited"></param>
        /// <param name="permissions"></param>
        /// <returns></returns>
        public async Task<ProtectedObjectDto> SetApiPermissionsAsync(string serviceName, string actionName, bool inherited, List<string> permissions)
        {
            var action = string.IsNullOrEmpty(actionName) ? "" : "@" + actionName;
            return await _protectedObjectManager.SetPermissionsAsync($"{serviceName}{action}", inherited, permissions);
        }

        /// <summary>
        /// Clear protected objects cache
        /// </summary>
        /// <returns></returns>
        public async Task ClearCacheAsync()
        {
            await _protectedObjectManager.ClearCacheAsync();
        }
    }
}