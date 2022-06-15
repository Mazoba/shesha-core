﻿using Abp.Events.Bus.Entities;
using Abp.Events.Bus.Handlers;
using Abp.Runtime.Caching;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Shesha.Domain;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Shesha.Swagger
{
    public class CachingSwaggerProvider : ISwaggerProvider, IEventHandler<EntityChangedEventData<EntityProperty>>
    {
        private readonly ICacheManager _cacheManager;

        private readonly SwaggerGenerator _swaggerGenerator;

        /// <summary>
        /// Cache of the ReferenceListItems
        /// </summary>
        protected ITypedCache<string, OpenApiDocument> SwaggerCache => _cacheManager.GetCache<string, OpenApiDocument>("SwaggerCache");

        public CachingSwaggerProvider(
            IOptions<SwaggerGeneratorOptions> optionsAccessor,
            IApiDescriptionGroupCollectionProvider apiDescriptionsProvider,
            ISchemaGenerator schemaGenerator,
            ICacheManager cacheManager)
        {
            _cacheManager = cacheManager;
            _swaggerGenerator = new SwaggerGenerator(optionsAccessor.Value, apiDescriptionsProvider, schemaGenerator);
        }

        public OpenApiDocument GetSwagger(string documentName, string host = null, string basePath = null)
        {
            return SwaggerCache.Get(documentName, (_) => _swaggerGenerator.GetSwagger(documentName, host, basePath));
        }

        public void HandleEvent(EntityChangedEventData<EntityProperty> eventData)
        {
            SwaggerCache.Clear();
        }
    }
}
