using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.ObjectMapping;
using NHibernate.Linq;
using Shesha.Web.FormsDesigner.Domain;
using Shesha.Web.FormsDesigner.Dtos;

namespace Shesha.Web.FormsDesigner.Services
{
    /// <summary>
    /// Sql form store. Uses DB table as a storage
    /// </summary>
    public class SqlFormStore: IFormStore, ITransientDependency
    {
        /// <summary>
        /// Reference to the object to object mapper.
        /// </summary>
        public IObjectMapper ObjectMapper { get; set; } = NullObjectMapper.Instance;

        private readonly IRepository<Form, Guid> _formRepository;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="formRepository"></param>
        public SqlFormStore(IRepository<Form, Guid> formRepository)
        {
            _formRepository = formRepository;
        }

        /// inheritedDoc
        public async Task<FormDto> GetAsync(Guid id)
        {
            var form = await _formRepository.GetAsync(id);
            return ObjectMapper.Map<FormDto>(form);
        }

        /// inheritedDoc
        public async Task<FormDto> UpdateAsync(FormDto formDto)
        {
            var form = await _formRepository.GetAsync(formDto.Id);
            ObjectMapper.Map(formDto, form);
            await _formRepository.UpdateAsync(form);

            return ObjectMapper.Map<FormDto>(form);
        }

        /// inheritedDoc
        public async Task<FormDto> CreateAsync(FormDto formDto)
        {
            var form = ObjectMapper.Map<Form>(formDto);
            await _formRepository.InsertAsync(form);

            return ObjectMapper.Map<FormDto>(form);
        }

        /// inheritedDoc
        public async Task<FormDto> GetByPathAsync(string path)
        {
            var form = await _formRepository.GetAll().FirstOrDefaultAsync(f => f.Path == path);
            return ObjectMapper.Map<FormDto>(form);
        }

        /// inheritedDoc
        public async Task<List<FormListItemDto>> AutocompleteAsync(string term, string selectedValue)
        {
            var forms = await _formRepository.GetAll()
                .Where(f => string.IsNullOrWhiteSpace(term) || f.Name.Contains(term, StringComparison.InvariantCultureIgnoreCase) || f.Path.Contains(term, StringComparison.InvariantCultureIgnoreCase))
                .OrderBy(f => f.Name)
                .Take(10)
                .ToListAsync();

            var result = forms
                .Select(f => new FormListItemDto
                {
                    Id = f.Id,
                    Path = f.Path,
                    Name = f.Name,
                })
                .ToList();

            return result;
        }
    }
}
