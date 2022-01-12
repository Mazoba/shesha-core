using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shesha.Extensions;
using System;
using System.Collections.Generic;

namespace Shesha.Application.Persons
{
    public class EntityBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Metadata.ModelType.IsEntityType())
            {
                var propertyBinders = new Dictionary<ModelMetadata, IModelBinder>();
                for (var i = 0; i < context.Metadata.Properties.Count; i++)
                {
                    var property = context.Metadata.Properties[i];
                    propertyBinders.Add(property, context.CreateBinder(property));
                }

                var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();
                return new EntityBinder(
                    propertyBinders,
                    loggerFactory,
                    allowValidatingTopLevelNodes: true);
            }

            return null;
        }
    }
}
