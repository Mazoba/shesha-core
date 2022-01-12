using Abp.Dependency;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NHibernate.Linq;
using Shesha.Domain;
using Shesha.Services;
using Shesha.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Shesha.Application.Persons
{

    public class EntityBinder : ComplexTypeModelBinder, IModelBinder, ITransientDependency
    {
        private const string JsonBodyCacheKey = "ShaJsonBodyCache";
        private readonly IRepository<Person, Guid> _personRepository;
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        //unit of work!

        public EntityBinder(IDictionary<ModelMetadata, IModelBinder> propertyBinders, ILoggerFactory loggerFactory, bool allowValidatingTopLevelNodes) : base(propertyBinders, loggerFactory, allowValidatingTopLevelNodes)
        {
            _personRepository = StaticContext.IocManager.Resolve<IRepository<Person, Guid>>();
            _unitOfWorkManager = StaticContext.IocManager.Resolve<IUnitOfWorkManager>();
        }

        public new async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var idStr = await GetValueAsync(bindingContext, nameof(Entity.Id));

            var uowExists = _unitOfWorkManager.Current != null;
            using (var uow = bindingContext.IsTopLevelObject ? _unitOfWorkManager.Begin() : null) 
            {
                // parse as concrete type
                if (Guid.TryParse(idStr, out var id))
                {
                    bindingContext.Model = await _personRepository.GetAll().FirstOrDefaultAsync(e => e.Id == id);
                }

                // call base binder
                await base.BindModelAsync(bindingContext);

                if (bindingContext.IsTopLevelObject)
                {
                    await uow.CompleteAsync();
                }
            }

            // bind configurable properties
        }

        private async Task<string> GetValueAsync(ModelBindingContext bindingContext, string propertyName) 
        {
            var value = GetValueFromValueProvider(bindingContext, propertyName);

            if (value == null)
                value = await GetValueFromJsonAsync(bindingContext, propertyName);

            if (value == null)
                value = await GetValueFromJsonAsync(bindingContext, propertyName.ToCamelCase());

            return value;
        }

        private string GetValueFromValueProvider(ModelBindingContext bindingContext, string propertyName) 
        {
            var fullPropertyName = ModelNames.CreatePropertyModelName(bindingContext.ModelName, propertyName);
            return bindingContext.ValueProvider.GetValue(fullPropertyName).FirstValue;
        }

        private async Task<string> GetValueFromJsonAsync(ModelBindingContext bindingContext, string propertyName)
        {
            var fullPropertyName = ModelNames.CreatePropertyModelName(bindingContext.ModelName, propertyName);
            
            var json = await GetJsonFromBodyAsync(bindingContext.ActionContext);

            return json.SelectToken(fullPropertyName)?.Value<string>();
        }

        private async Task<JObject> GetJsonFromBodyAsync(ActionContext actionContext)
        {
            var json = actionContext.HttpContext.Items[JsonBodyCacheKey] as JObject;
            if (json == null) 
            {
                var jsonText = await ExtractRequestJsonAsync(actionContext);
                json = JObject.Parse(jsonText);
                actionContext.HttpContext.Items[JsonBodyCacheKey] = json;
            }

            return json;
        }

        private static async Task<string> ExtractRequestJsonAsync(ActionContext actionContext)
        {
            var body = actionContext.HttpContext.Request.Body;
            if (body == null)
                return null;

            body.Position = 0;

            using (var reader = new StreamReader(body, leaveOpen: true))
            {
                var content = await reader.ReadToEndAsync();
                body.Position = 0;

                return content;
            }
        }
    }
}
