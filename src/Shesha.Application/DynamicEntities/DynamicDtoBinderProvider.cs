using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Shesha.DynamicEntities
{
    public class DynamicDtoBinderProvider : IModelBinderProvider
    {
        private readonly IList<IInputFormatter> _formatters;
        private readonly IHttpRequestStreamReaderFactory _readerFactory;
        private readonly ILoggerFactory _loggerFactory;
        private readonly MvcOptions? _options;

        /// <summary>
        /// Creates a new <see cref="DynamicDtoBinderProvider"/>.
        /// </summary>
        /// <param name="formatters">The list of <see cref="IInputFormatter"/>.</param>
        /// <param name="readerFactory">The <see cref="IHttpRequestStreamReaderFactory"/>.</param>
        public DynamicDtoBinderProvider(IList<IInputFormatter> formatters, IHttpRequestStreamReaderFactory readerFactory)
            : this(formatters, readerFactory, loggerFactory: Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance)
        {
        }

        /// <summary>
        /// Creates a new <see cref="DynamicDtoBinderProvider"/>.
        /// </summary>
        /// <param name="formatters">The list of <see cref="IInputFormatter"/>.</param>
        /// <param name="readerFactory">The <see cref="IHttpRequestStreamReaderFactory"/>.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public DynamicDtoBinderProvider(IList<IInputFormatter> formatters, IHttpRequestStreamReaderFactory readerFactory, ILoggerFactory loggerFactory)
            : this(formatters, readerFactory, loggerFactory, options: null)
        {
        }

        /// <summary>
        /// Creates a new <see cref="DynamicDtoBinderProvider"/>.
        /// </summary>
        /// <param name="formatters">The list of <see cref="IInputFormatter"/>.</param>
        /// <param name="readerFactory">The <see cref="IHttpRequestStreamReaderFactory"/>.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        /// <param name="options">The <see cref="MvcOptions"/>.</param>
        public DynamicDtoBinderProvider(
            IList<IInputFormatter> formatters,
            IHttpRequestStreamReaderFactory readerFactory,
            ILoggerFactory loggerFactory,
            MvcOptions? options)
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
            _readerFactory = readerFactory;
            _loggerFactory = loggerFactory;
            _options = options;
        }

        /// <inheritdoc />
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.BindingInfo.BindingSource != null &&
                context.BindingInfo.BindingSource.CanAcceptDataFrom(BindingSource.Body) &&
                context.Metadata.ModelType.IsDynamicDto())
            {
                if (_formatters.Count == 0)
                {
                    throw new InvalidOperationException($"'{typeof(MvcOptions).FullName}.{nameof(MvcOptions.InputFormatters)}' must not be empty. At least one '{typeof(IInputFormatter).FullName}' is required to bind from the body.");
                }

                var treatEmptyInputAsDefaultValue = CalculateAllowEmptyBody(context.BindingInfo.EmptyBodyBehavior, _options);

                return new DynamicDtoModelBinder(_formatters, _readerFactory, _loggerFactory, _options)
                {
                    AllowEmptyBody = treatEmptyInputAsDefaultValue,
                };
            }

            return null;
        }

        internal static bool CalculateAllowEmptyBody(EmptyBodyBehavior emptyBodyBehavior, MvcOptions? options)
        {
            if (emptyBodyBehavior == EmptyBodyBehavior.Default)
            {
                return options?.AllowEmptyInputInBodyModelBinding ?? false;
            }

            return emptyBodyBehavior == EmptyBodyBehavior.Allow;
        }
    }
}