using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Shesha.AutoMapper.Dto;

namespace Shesha.Web.DataTable
{
    /// <summary>
    /// DataTable Application Service
    /// </summary>
    public class NewDataTableAppService : IDataTableAppService
    {
        private readonly IDataTableConfigurationStore _configurationStore;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="configurationStore"></param>
        public NewDataTableAppService(IDataTableConfigurationStore configurationStore)
        {
            _configurationStore = configurationStore;
        }

        /// <summary>
        /// Get identifiers of all registered table configurations
        /// </summary>
        /// <returns></returns>
        public List<string> GetTableIds()
        {
            return _configurationStore.GetTableIds();
        }

        /// inhertiedDoc
        [HttpGet]
        public List<AutocompleteItemDto> TableIdAutocomplete(string term, string selectedValue)
        {
            var isPreselection = string.IsNullOrWhiteSpace(term) && !string.IsNullOrWhiteSpace(selectedValue);
            if (isPreselection)
            {
                return new List<AutocompleteItemDto> {
                    new AutocompleteItemDto {
                        Value = selectedValue,
                        DisplayText = selectedValue
                    }
                };
            }

            return _configurationStore.GetTableIds()
                .Where(i => string.IsNullOrWhiteSpace(term) || i.ToLower().Contains(term.ToLower()))
                .OrderBy(i => i)
                .Take(10)
                .Select(i => new AutocompleteItemDto
                {
                    Value = i,
                    DisplayText = i
                })
                .ToList();
        }
    }
}
