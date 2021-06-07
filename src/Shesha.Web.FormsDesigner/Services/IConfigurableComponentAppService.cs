using System;
using System.Threading.Tasks;
using Shesha.Web.FormsDesigner.Dtos;

namespace Shesha.Web.FormsDesigner.Services
{
    /// <summary>
    /// Configurable component application service
    /// </summary>
    public interface IConfigurableComponentAppService
    {
        /// <summary>
        /// Get dto
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<ConfigurableComponentDto> GetAsync(Guid id);

        /// <summary>
        /// Update dto
        /// </summary>
        /// <returns></returns>
        Task<ConfigurableComponentDto> UpdateAsync(ConfigurableComponentDto dto);

        /// <summary>
        /// Create new dto
        /// </summary>
        /// <returns></returns>
        Task<ConfigurableComponentDto> CreateAsync(ConfigurableComponentDto dto);

        /// <summary>
        /// Update dto markup
        /// </summary>
        /// <returns></returns>
        Task UpdateSettingsAsync(ConfigurableComponentUpdateSettingsInput input);
    }
}
