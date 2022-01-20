using Abp.TestBase;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Newtonsoft.Json;
using Shesha.Domain;
using Shesha.DynamicEntities;
using Shesha.DynamicEntities.Dtos;
using Shesha.Tests.DynamicEntities.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Shouldly;
using Shesha.DynamicEntities.Cache;
using Shesha.Tests.DynamicEntities.Dtos;
using Shesha.Metadata;

namespace Shesha.Tests.DynamicEntities
{
    public class DynamicDtoModelBinder_Tests : AbpIntegratedTestBase<SheshaTestModule>
    {
        [Fact]
        public async Task Bind2NestedLevels_Test() 
        {
            var bindingResult = await BindAsync<PersonDynamicDto>("nested2Levels.json", "nested2Levels.metadata.json");

            // Assert
            Assert.True(bindingResult.IsModelSet);

            var model = bindingResult.Model;

            const string addressPropertyName = "NestedDynamicAddress";
            const string addressLine1PropertyName = "Line1";

            var nestedAddressProperty = model.GetType().GetProperty(addressPropertyName);
            nestedAddressProperty.ShouldNotBeNull($"'{addressPropertyName}' property is missing in the model");

            var nestedAddressValue = nestedAddressProperty.GetValue(model);
            nestedAddressValue.ShouldNotBeNull($"'{addressPropertyName}' property must not be null");

            var line1Property = nestedAddressValue.GetType().GetProperty(addressLine1PropertyName);
            line1Property.ShouldNotBeNull($"'{addressLine1PropertyName}' property is missing in the nested '{addressPropertyName}' property");

            var line1Value = line1Property.GetValue(nestedAddressValue);
            line1Value.ShouldBe("address line 1");
        }

        [Fact]
        public async Task Bind3NestedLevels_Test()
        {
            var bindingResult = await BindAsync<PersonDynamicDto>("nested3Levels.json", "nested3Levels.metadata.json");

            // Assert
            Assert.True(bindingResult.IsModelSet);

            var model = bindingResult.Model;

            const string addressPropertyName = "NestedDynamicAddress";
            const string addressLine1PropertyName = "Line1";

            var nestedAddressProperty = model.GetType().GetProperty(addressPropertyName);
            nestedAddressProperty.ShouldNotBeNull($"'{addressPropertyName}' property is missing in the model");

            var nestedAddressValue = nestedAddressProperty.GetValue(model);
            nestedAddressValue.ShouldNotBeNull($"'{addressPropertyName}' property must not be null");

            var line1Property = nestedAddressValue.GetType().GetProperty(addressLine1PropertyName);
            line1Property.ShouldNotBeNull($"'{addressLine1PropertyName}' property is missing in the nested '{addressPropertyName}' property");

            var line1Value = line1Property.GetValue(nestedAddressValue);
            line1Value.ShouldBe("address line 1");

            const string thirdLevelPropName = "ThirdLevelProp";
            var thirdLevelProperty = nestedAddressValue.GetType().GetProperty(thirdLevelPropName);
            thirdLevelProperty.ShouldNotBeNull($"'{thirdLevelPropName}' property is missing in the nested '{addressPropertyName}' property");

            var thirdLevelPropValue = thirdLevelProperty.GetValue(nestedAddressValue);
            thirdLevelPropValue.ShouldNotBeNull();
        }

        private async Task<ModelBindingResult> BindAsync<TModel>(string jsonResourceName, string schemaResourceName) 
        {
            // Arrange
            var mockInputFormatter = new Mock<IInputFormatter>();
            mockInputFormatter.Setup(f => f.CanRead(It.IsAny<InputFormatterContext>()))
                .Returns(true)
                .Verifiable();
            mockInputFormatter.Setup(o => o.ReadAsync(It.IsAny<InputFormatterContext>()))
                              .Returns(async (InputFormatterContext context) => {
                                  var model = await ReadJsonRequestAsync(context.ModelType, jsonResourceName);
                                  return InputFormatterResult.Success(model);
                              })
                              .Verifiable();
            var inputFormatter = mockInputFormatter.Object;

            var bindingContext = GetBindingContext(
                typeof(TModel));

            var mockDtoBuilder = await GetDtoBuilderAsync(schemaResourceName);

            var binder = CreateBinder(new[] { inputFormatter }, mockDtoBuilder);

            // Act
            await binder.BindModelAsync(bindingContext);

            // Assert
            mockInputFormatter.Verify(v => v.CanRead(It.IsAny<InputFormatterContext>()), Times.Once);
            mockInputFormatter.Verify(v => v.ReadAsync(It.IsAny<InputFormatterContext>()), Times.Once);
            Assert.True(bindingContext.Result.IsModelSet);

            return bindingContext.Result;
        }

        #region private methods

        private async Task<object> ReadJsonRequestAsync(Type modelType, string jsonResourceName) 
        {
            var content = await GetResourceStringAsync($"{this.GetType().Namespace}.Resources.{jsonResourceName}", this.GetType().Assembly);
            var deserialized = JsonConvert.DeserializeObject(content, modelType);
            return deserialized;
        }

        private async Task<string> GetResourceStringAsync(string resourceName, Assembly assembly)
        {
            using (var stream = assembly.GetManifestResourceStream(resourceName)) 
            {
                using (var sr = new StreamReader(stream))
                {
                    return await sr.ReadToEndAsync();
                }
            }
        }

        private static DynamicDtoModelBinder CreateBinder(IList<IInputFormatter> formatters, IDynamicDtoTypeBuilder dtoBuilder)
        {
            var options = new MvcOptions();
            var binder = CreateBinder(formatters, options, dtoBuilder);
            //binder.AllowEmptyBody = treatEmptyInputAsDefaultValueOption;

            return binder;
        }

        private static DynamicDtoModelBinder CreateBinder(IList<IInputFormatter> formatters, MvcOptions mvcOptions, IDynamicDtoTypeBuilder dtoBuilder)
        {
            return new DynamicDtoModelBinder(formatters, new TestHttpRequestStreamReaderFactory(), NullLoggerFactory.Instance, mvcOptions, dtoBuilder);
        }

        private static DefaultModelBindingContext GetBindingContext(
            Type modelType,
            HttpContext httpContext = null,
            IModelMetadataProvider metadataProvider = null)
        {
            if (httpContext == null)
            {
                httpContext = new DefaultHttpContext();
            }

            if (metadataProvider == null)
            {
                metadataProvider = new EmptyModelMetadataProvider();
            }

            var bindingContext = new DefaultModelBindingContext
            {
                ActionContext = new ActionContext()
                {
                    HttpContext = httpContext,
                },
                //FieldName = "someField",
                IsTopLevelObject = true,
                ModelMetadata = metadataProvider.GetMetadataForType(modelType),
                ModelName = "",
                ValueProvider = Mock.Of<IValueProvider>(),
                ModelState = new ModelStateDictionary(),
                BindingSource = BindingSource.Body,
            };

            return bindingContext;
        }

        private async Task<IDynamicDtoTypeBuilder> GetDtoBuilderAsync(string schemaResourceName)
        {
            var entityConfigCacheMock = new Mock<IEntityConfigCache>();

            entityConfigCacheMock.Setup(x => x.GetEntityPropertiesAsync(It.IsAny<Type>()))
                .Returns(async () => {
                    var schema = await ReadJsonRequestAsync(typeof(List<EntityPropertyDto>), schemaResourceName) as List<EntityPropertyDto>;
                    return schema;
                });

            return new DynamicDtoTypeBuilder(entityConfigCacheMock.Object);
        }

        #endregion

        public class PersonDynamicDto : DynamicDto<Person, Guid>
        {
            public string FirstName { get; set; }
            public string lastName { get; set; }
        }
    }
}
