using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shesha.Permissions.Enum;

namespace Shesha.Permissions
{
    public interface IProtectedObjectManager
    {
        /// <summary>
        /// Get category of Protected Object for Type or NULL if not protected
        /// </summary>
        /// <param name="type">Type</param>
        /// <returns></returns>
        string GetCategoryByType(Type type);

        /// <summary>
        /// Get list of protected objects
        /// </summary>
        /// <param name="category">Filter by Category</param>
        /// <param name="showHidden">Show hidden protected objects</param>
        /// <returns></returns>
        Task<List<ProtectedObjectDto>> GetAllFlatAsync(string category, bool showHidden);

        /// <summary>
        /// Get hierarchical list of protected objects
        /// </summary>
        /// <param name="category">Filter by Category</param>
        /// <param name="showHidden">Show hidden protected objects</param>
        /// <returns></returns>
        Task<List<ProtectedObjectDto>> GetAllTreeAsync(string category, bool showHidden);

        /// <summary>
        /// Get Protected Object by object name with children
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="showHidden"></param>
        /// <returns></returns>
        Task<ProtectedObjectDto> GetObjectWithChild(string objectName, bool showHidden);

        /// <summary>
        /// Get Protected Object by object name
        /// </summary>
        /// <param name="objectName">Object name for search Protected Object (usually it has format "type@action")</param>
        /// <param name="useInherited">Get permission data from parent if inherited</param>
        /// <param name="useDependency">Get permission data from related Protected Object if it specified</param>
        /// <param name="useHidden">Allow to get permission data from hidden protected objects</param>
        /// <returns></returns>
        Task<ProtectedObjectDto> GetAsync(string objectName, bool useInherited, UseDependencyType useDependency, bool useHidden);

        /// <summary>
        /// Set Protected Object data (save to DB and cache)
        /// </summary>
        /// <param name="protectedObject">Protected Object data</param>
        /// <returns></returns>
        Task<ProtectedObjectDto> SetAsync(ProtectedObjectDto protectedObject);

        /// <summary>
        /// Set permission data for Protected Object by object name
        /// </summary>
        /// <param name="objectName">Object name for search Protected Object (usually it has format "type@action")</param>
        /// <param name="inherited">Get permission data from the parent Protected Object if value is True</param>
        /// <param name="permissions">Required permissions for Protected Object. Will be ignored if Inherited is True</param>
        /// <returns></returns>
        Task<ProtectedObjectDto> SetPermissionsAsync(string objectName, bool inherited, List<string> permissions);

        /// <summary>
        /// Clear protected objects cache
        /// </summary>
        /// <returns></returns>
        Task ClearCacheAsync();
    }
}