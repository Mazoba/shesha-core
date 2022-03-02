using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shesha.AutoMapper.Dto;
using Shesha.Web.DataTable;
using Shesha.Web.FormsDesigner.Domain;
using Shesha.Web.FormsDesigner.Dtos;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Shesha.Web.FormsDesigner.Services
{
    /// <summary>
    /// Configurable Forms application service
    /// </summary>
    //[AbpAuthorize]
    [Route("api/services/Forms")]
    public class FormAppService : SheshaAppServiceBase, IFormAppService
    {
        private readonly IFormStore _formStore;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="formStore"></param>
        public FormAppService(IFormStore formStore)
        {
            _formStore = formStore;
        }

        /// <summary>
        /// Index table configuration. Note: It's just a temporary solution 
        /// </summary>
        public static DataTableConfig IndexTable()
        {
            var table = new DataTableConfig<Form, Guid>("Forms_Index");

            table.AddProperty(e => e.Name);
            table.AddProperty(e => e.Path);
            table.AddProperty(e => e.Description);
            table.AddProperty(e => e.ModelType);
            table.AddProperty(e => e.Settings);

            table.AddProperty(e => e.CreationTime, c => c.Caption("Created On").HiddenByDefault());
            table.AddProperty(e => e.LastModificationTime, c => c.Caption("Updated On").HiddenByDefault());

            return table;
        }

        /// inheritedDoc
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

        /// inheritedDoc
        [HttpGet, Route("")]
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
        [HttpPut, Route("")]
        public async Task<FormDto> UpdateAsync(FormDto form)
        {
            return await _formStore.UpdateAsync(form);
        }

        /// inheritedDoc
        [HttpPost, Route("")]
        public async Task<FormDto> CreateAsync(FormDto form)
        {
            var dto = await _formStore.CreateAsync(form);
            return dto;
        }

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

        [HttpPost, Route("Export/Default")]
        public async Task<FileContentResult> ExportConfigurationsAsyncDefault()
        {
            var fileName = "formConfigExport_" + DateTime.Now;
            var mimeType = "application/json";

            try
            {
                //Loop through the list of IDs, 
                // for each ID get form configs from DB, then append to JSON
                var configList = new List<Dictionary<string, string>>();
                var forms = await _formStore.GetAllAsync();

                foreach (var config in forms)
                {

                    var configDictionary = new Dictionary<string, string>();
                    configDictionary.Add("Path", config.Path);
                    configDictionary.Add("Name", config.Name);
                    configDictionary.Add("Description", config.Description);
                    configDictionary.Add("Markup", config.Markup);
                    configDictionary.Add("ModelType", config.ModelType);
                    configDictionary.Add("Type", config.Type);
                    configList.Add(configDictionary);

                }

                byte[] fileBytes = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(configList));


                return new FileContentResult(fileBytes, mimeType)
                {
                    FileDownloadName = fileName
                };

            }
            catch (Exception e)
            {
                Logger.Error("An error occurred", e);
                throw new Exception("An error occurred!");
            }

        }

        [HttpPost, Route("Export")]
        public async Task<FileContentResult> ExportConfigurationsAsync(ExportConfigurationDto ids)
        {
            var fileName = "formConfigExport_" + DateTime.Now;
            var mimeType = "application/json";

            try
            {
                //Loop through the list of IDs, 
                // for each ID get form configs from DB, then append to JSON
                var configList = new List<Dictionary<string, string>>();

                foreach (var id in ids.Components)
                {
                    var forms = await _formStore.GetAsync(id);

                    var configDictionary = new Dictionary<string, string>();
                    configDictionary.Add("Path", forms.Path);
                    configDictionary.Add("Name", forms.Name);
                    configDictionary.Add("Description", forms.Description);
                    configDictionary.Add("Markup", forms.Markup);
                    configDictionary.Add("ModelType", forms.ModelType);
                    configDictionary.Add("Type", forms.Type);
                    configList.Add(configDictionary);

                }

                byte[] fileBytes = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(configList));


                return new FileContentResult(fileBytes, mimeType)
                {
                    FileDownloadName = fileName
                };

            }
            catch (Exception e)
            {
                Logger.Error("An error occurred", e);
                throw new Exception("An error occurred!");
            }

        }

        [HttpPost, Route("Import")]
        [Consumes("multipart/form-data")]
        public async Task<string> ImportConfigurationAsync([FromForm] ImportConfigDto importConfig)
        {
            IFormFile formFile = importConfig.File;

            if (formFile.Length > 0)
            {
                var result = new StringBuilder();
                using (var reader = new StreamReader(formFile.OpenReadStream()))
                {
                    while (reader.Peek() >= 0)
                        result.AppendLine(await reader.ReadLineAsync());
                }
  

               var deserialisedJson = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(result.ToString());

                foreach(var config in deserialisedJson)
                {
                    //Create FormDTO and validate
                    string formPath = config.GetValueOrDefault("Path");

                    var existingFormPath = await _formStore.GetByPathAsync(formPath);

                    if (existingFormPath == null)
                    {
                        FormDto form = GenerateFormDtoHelper(config, formPath);

                        await _formStore.CreateAsync(form);
                        Logger.Info("Config added successfully!");

                    }
                    else
                    {
                        //Handle scenario that the path already exists
                        Logger.Info("Duplicate config!");
                        //Overide the config
                        Logger.Info("Overwriting them");
                        FormDto form = GenerateFormDtoHelper(config, formPath);

                        await _formStore.CreateAsync(form);
                        Logger.Info("Config added successfully!");
                    }


                                     
                }

                return "Import success!";

            }
            else
            {
                return "File Import Failed. An error occured!";
            }
           
        }

        private static FormDto GenerateFormDtoHelper(Dictionary<string, string> config, string formPath)
        {
            FormDto form = new FormDto();
            form.Path = formPath;
            form.Markup = config.GetValueOrDefault("Markup");
            form.Name = config.GetValueOrDefault("Name");
            form.ModelType = config.GetValueOrDefault("ModelType");
            form.Description = config.GetValueOrDefault("Description");
            return form;
        }
    }

}
