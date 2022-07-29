using Abp.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;
using Shesha.Web.FormsDesigner.Domain;
using Shesha.Web.FormsDesigner.Dtos;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Shesha.Web.FormsDesigner.Services
{
    /// <summary>
    /// Configurable Forms application service
    /// </summary>
    //[Route("api/services/Forms")]
    public class FormAppService : SheshaCrudServiceBase<Form, FormDto, Guid>//SheshaAppServiceBase, IFormAppService
    {
        private readonly IFormStore _formStore;

        public FormAppService(IRepository<Form, Guid> repository, IFormStore formStore) : base(repository)
        {
            _formStore = formStore;
        }

        /// inheritedDoc
        /*
        [HttpGet, Route("{id}")]
        public async Task<FormDto> GetAsync(Guid id)
        {
            var form = await _formStore.GetAsync(id);

            // Note: temporary stuff, is used for development only, to be removed
            if (!string.IsNullOrWhiteSpace(form.Path) && File.Exists(form.Path))
            {
                form.Markup = await File.ReadAllTextAsync(form.Path);
            }

            return form;
        }
        */

        /*
        /// inheritedDoc
        [HttpPut, Route("{id}/Markup")]
        public async Task UpdateMarkupAsync(FormUpdateMarkupInput input)
        {
            var form = await _formStore.GetAsync(input.Id);
            form.Markup = input.Markup;
            await _formStore.UpdateAsync(form);

            // update physical json file on disk
            // Note: it's only for development, to be removed later!
            // I use this just to simplify component properties form
            if (!string.IsNullOrWhiteSpace(form.Path))
            {
                try
                {
                    if (File.Exists(form.Path))
                    {
                        await File.WriteAllTextAsync(form.Path, input.Markup);
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to save form json to file", e);
                }
            }
        }
        */

        /*
        /// inheritedDoc
        [HttpGet, Route("autocomplete")]
        public async Task<List<AutocompleteItemDto>> AutocompleteAsync(string term, string selectedValue)
        {
            var items = await _formStore.AutocompleteAsync(term, selectedValue);

            var result = items
                .Select(i => new AutocompleteItemDto
                {
                    Value = i.Path,
                    DisplayText = $"{i.Name} ({i.Path})"
                })
                .ToList();

            return result;
        }
        */

//        [HttpGet, Route("")]
        public async Task<FormDto> GetByPathAsync(string path)
        {
            var form = await _formStore.GetByPathAsync(path);

            // temporary stuff
            if (form == null)
            {
                form = await _formStore.CreateAsync(new FormDto()
                {
                    Path = path,
                    Name = path,
                });
                /*
                form = await _formStore.GetByPathAsync(path);
                if (form == null)
                    throw new Exception("Failed to create a form");
                */
            }

            // Note: temporary stuff, is used for development only, to be removed
            if (!string.IsNullOrWhiteSpace(form.Path) && File.Exists(form.Path))
            {
                form.Markup = await File.ReadAllTextAsync(form.Path);
            }

            return form;
        }

        /// inheritedDoc
//        [HttpPut, Route("{id}/Markup")]
        public async Task UpdateMarkupAsync(FormUpdateMarkupInput input)
        {
            var form = await _formStore.GetAsync(input.Id);
            form.Markup = input.Markup;
            await _formStore.UpdateAsync(form);

            // update physical json file on disk
            // Note: it's only for development, to be removed later!
            // I use this just to simplify component properties form
            if (!string.IsNullOrWhiteSpace(form.Path))
            {
                try
                {
                    if (File.Exists(form.Path))
                    {
                        await File.WriteAllTextAsync(form.Path, input.Markup);
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("Failed to save form json to file", e);
                }
            }
        }
    }
}
