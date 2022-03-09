﻿using AutoMapper;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Shesha.DynamicEntities.Dtos;
using Shesha.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Shesha.DynamicEntities
{
    /// <summary>
    /// Model Binder that binds dynamic DTOs
    /// It creates proxy types with dynamic properties according to the entity configuration
    /// </summary>
    public class DynamicDtoModelBinder : IModelBinder
    {
        private readonly IList<IInputFormatter> _formatters;
        private readonly Func<Stream, Encoding, TextReader> _readerFactory;
        private readonly ILogger _logger;
        private readonly MvcOptions? _options;
        private readonly IDynamicDtoTypeBuilder _dtoBuilder;

        /// <summary>
        /// Creates a new <see cref="DynamicDtoModelBinder"/>.
        /// </summary>
        /// <param name="formatters">The list of <see cref="IInputFormatter"/>.</param>
        /// <param name="readerFactory">
        /// The <see cref="IHttpRequestStreamReaderFactory"/>, used to create <see cref="System.IO.TextReader"/>
        /// instances for reading the request body.
        /// </param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        /// <param name="dynamicDtoTypeBuilder">Dynamic DTO builder</param>
        public DynamicDtoModelBinder(
            IList<IInputFormatter> formatters,
            IHttpRequestStreamReaderFactory readerFactory,
            ILoggerFactory? loggerFactory,
            IDynamicDtoTypeBuilder dynamicDtoTypeBuilder)
            : this(formatters, readerFactory, loggerFactory, options: null, dynamicDtoTypeBuilder)
        {
        }

        /// <summary>
        /// Creates a new <see cref="DynamicDtoModelBinder"/>.
        /// </summary>
        /// <param name="formatters">The list of <see cref="IInputFormatter"/>.</param>
        /// <param name="readerFactory">
        /// The <see cref="IHttpRequestStreamReaderFactory"/>, used to create <see cref="System.IO.TextReader"/>
        /// instances for reading the request body.
        /// </param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        /// <param name="options">The <see cref="MvcOptions"/>.</param>
        public DynamicDtoModelBinder(
            IList<IInputFormatter> formatters,
            IHttpRequestStreamReaderFactory readerFactory,
            ILoggerFactory? loggerFactory,
            MvcOptions? options,
            IDynamicDtoTypeBuilder dynamicDtoTypeBuilder)
        {
            if (formatters == null)
            {
                throw new ArgumentNullException(nameof(formatters));
            }

            if (readerFactory == null)
            {
                throw new ArgumentNullException(nameof(readerFactory));
            }

            _formatters = formatters;
            _readerFactory = readerFactory.CreateReader;

            _logger = loggerFactory?.CreateLogger<DynamicDtoModelBinder>() ?? NullLogger<DynamicDtoModelBinder>.Instance;

            _options = options;
            _dtoBuilder = dynamicDtoTypeBuilder;
        }

        internal bool AllowEmptyBody { get; set; }

        /// <inheritdoc />
        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            _logger.AttemptingToBindModel(bindingContext);

            // Special logic for body, treat the model name as string.Empty for the top level
            // object, but allow an override via BinderModelName. The purpose of this is to try
            // and be similar to the behavior for POCOs bound via traditional model binding.
            string modelBindingKey;
            if (bindingContext.IsTopLevelObject)
            {
                modelBindingKey = bindingContext.BinderModelName ?? string.Empty;
            }
            else
            {
                modelBindingKey = bindingContext.ModelName;
            }

            var httpContext = bindingContext.HttpContext;

            #region 

            // check if type is already proxied
            var modelType = bindingContext.ModelType;
            var metadata = bindingContext.ModelMetadata;

            if (modelType is IDynamicDtoProxy)
                throw new NotSupportedException($"{this.GetType().FullName} doesn't support binding of the dynamic poxies. Type `{modelType.FullName}` is implementing `{nameof(IDynamicDtoProxy)}` interface");

            var fullDtoBuildContext = new DynamicDtoTypeBuildingContext
            {
                ModelType = bindingContext.ModelType,
                PropertyFilter = propName => true,
                AddFormFieldsProperty = true,
            };
            modelType = await _dtoBuilder.BuildDtoFullProxyTypeAsync(bindingContext.ModelType, fullDtoBuildContext);

            metadata = bindingContext.ModelMetadata.GetMetadataForType(modelType);
            
            #endregion

            var formatterContext = new InputFormatterContext(
                httpContext,
                modelBindingKey,
                bindingContext.ModelState,
                metadata,
                _readerFactory,
                AllowEmptyBody);

            var formatter = (IInputFormatter?)null;
            for (var i = 0; i < _formatters.Count; i++)
            {
                if (_formatters[i].CanRead(formatterContext))
                {
                    formatter = _formatters[i];
                    _logger.InputFormatterSelected(formatter, formatterContext);
                    break;
                }
                else
                {
                    _logger.InputFormatterRejected(_formatters[i], formatterContext);
                }
            }

            if (formatter == null)
            {
                if (AllowEmptyBody)
                {
                    var hasBody = httpContext.Features.Get<IHttpRequestBodyDetectionFeature>()?.CanHaveBody;
                    hasBody ??= httpContext.Request.ContentLength is not null && httpContext.Request.ContentLength == 0;
                    if (hasBody == false)
                    {
                        bindingContext.Result = ModelBindingResult.Success(model: null);
                        return;
                    }
                }

                _logger.NoInputFormatterSelected(formatterContext);

                var message = $"Unsupported content type '{httpContext.Request.ContentType}'.";
                var exception = new UnsupportedContentTypeException(message);
                bindingContext.ModelState.AddModelError(modelBindingKey, exception, bindingContext.ModelMetadata);
                _logger.DoneAttemptingToBindModel(bindingContext);
                return;
            }

            try
            {
                var result = await formatter.ReadAsync(formatterContext);

                if (result.HasError)
                {
                    // Formatter encountered an error. Do not use the model it returned.
                    _logger.DoneAttemptingToBindModel(bindingContext);
                    return;
                }

                if (result.IsModelSet)
                {
                    if (result.Model is IHasFormFieldsList modelWithFormFields) 
                    {
                        //var formFields = modelWithFormFields._formFields.Select(f => f.ToLower()).ToList();
                        var bindKeys = GetAllPropertyKeys(modelWithFormFields._formFields);

                        var buildContext = new DynamicDtoTypeBuildingContext {
                            ModelType = bindingContext.ModelType,
                            PropertyFilter = propName => {
                                return bindKeys.Contains(propName.ToLower());
                            },
                            AddFormFieldsProperty = true,
                        };
                        var effectiveModelType = await _dtoBuilder.BuildDtoProxyTypeAsync(buildContext);
                        var mapper = GetMapper(result.Model.GetType(), effectiveModelType, fullDtoBuildContext.Classes);
                        var effectiveModel = mapper.Map(result.Model, result.Model.GetType(), effectiveModelType);
                        
                        bindingContext.Result = ModelBindingResult.Success(effectiveModel);
                    } else
                        bindingContext.Result = ModelBindingResult.Success(result.Model);

                    // map results (form manual bindings only)
                    /*
                    if (bindingContext.Model != null && result.Model != null) 
                    {
                        var mapper = GetMapper(result.Model.GetType(), bindingContext.Model.GetType());
                        mapper.Map(result.Model, bindingContext.Model);
                    }
                    */
                }
                else
                {
                    // If the input formatter gives a "no value" result, that's always a model state error,
                    // because BodyModelBinder implicitly regards input as being required for model binding.
                    // If instead the input formatter wants to treat the input as optional, it must do so by
                    // returning InputFormatterResult.Success(defaultForModelType), because input formatters
                    // are responsible for choosing a default value for the model type.
                    var message = bindingContext
                        .ModelMetadata
                        .ModelBindingMessageProvider
                        .MissingRequestBodyRequiredValueAccessor();
                    bindingContext.ModelState.AddModelError(modelBindingKey, message);
                }
            }
            catch (Exception exception) when (exception is InputFormatterException || ShouldHandleException(formatter))
            {
                bindingContext.ModelState.AddModelError(modelBindingKey, exception, bindingContext.ModelMetadata);
            }

            _logger.DoneAttemptingToBindModel(bindingContext);
        }

        private List<string> GetAllPropertyKeys(List<string> fieldsList) 
        {
            var result = new List<string>();

            foreach (var field in fieldsList) 
            {
                var parts = field.ToLower().Split('.');
                var path = "";
                foreach (var part in parts) 
                {
                    path = string.IsNullOrWhiteSpace(path)
                        ? part
                        : path + "." + part;

                    if (!result.Contains(path))
                        result.Add(path);
                }
            }

            return result;
        }

        private IMapper GetMapper(Type sourceType, Type destinationType, Dictionary<string, Type> classes)
        {
            var nestedTypes = new Dictionary<Type, Type>();
            foreach (var srcClassInfo in classes) 
            {
                var srcType = srcClassInfo.Value;
                if (typeof(IDynamicNestedObject).IsAssignableFrom(srcType))
                {
                    var path = srcClassInfo.Key;
                    var dstProperty = FindProperty(destinationType, path);
                    if (dstProperty != null)
                    {
                        nestedTypes.Add(srcType, dstProperty.PropertyType);
                    }
                }
            }

            var modelConfigMapperConfig = new MapperConfiguration(cfg => {
                var mapExpression = cfg.CreateMap(sourceType, destinationType);

                foreach (var srcType in nestedTypes.Keys)
                    cfg.CreateMap(srcType, nestedTypes[srcType]);
            });

            return modelConfigMapperConfig.CreateMapper();
        }

        private PropertyInfo FindProperty(Type type, string path) 
        {
            PropertyInfo currentProperty = null;
            var currentType = type;

            var pathParts = path.Split('.');
            foreach (var pathPart in pathParts) 
            {
                var property = currentType.GetProperty(pathPart);

                if (property == null)
                    return null;

                currentType = property.PropertyType;
                currentProperty = property;
            }

            return currentProperty;
        }

        private bool ShouldHandleException(IInputFormatter formatter)
        {
            // Any explicit policy on the formatters overrides the default.
            var policy = (formatter as IInputFormatterExceptionPolicy)?.ExceptionPolicy ??
                InputFormatterExceptionPolicy.MalformedInputExceptions;

            return policy == InputFormatterExceptionPolicy.AllExceptions;
        }
    }
}
;