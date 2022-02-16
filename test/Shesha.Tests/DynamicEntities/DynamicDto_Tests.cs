﻿using Abp.TestBase;
using Moq;
using Shesha.Domain;
using Shesha.DynamicEntities;
using Shesha.DynamicEntities.Cache;
using Shesha.DynamicEntities.Dtos;
using Shesha.Metadata;
using Shesha.Tests.DynamicEntities.Dtos;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Shesha.Tests.DynamicEntities
{
    public class DynamicDto_Tests : AbpIntegratedTestBase<SheshaTestModule>//SheshaNhTestBase
    {
        [Fact]
        public async Task BuildFullDynamicDto_Test()
        {
            var entityConfigCacheMock = new Mock<IEntityConfigCache>();
            
            entityConfigCacheMock.Setup(x => x.GetEntityPropertiesAsync(It.IsAny<Type>()))
                .Returns(() => {
                    var result = new EntityPropertyDtoList();
                    result.AddString("Name", "Name...");
                    result.AddString("Description", "Description...");
                    

                    var r = result as List<EntityPropertyDto>;
                    return Task.FromResult(r);
                });

            var builder = new DynamicDtoTypeBuilder(entityConfigCacheMock.Object);

            var baseDtoType = typeof(DynamicDto<Person, Guid>);

            var context = new DynamicDtoTypeBuildingContext() { ModelType = baseDtoType };
            var proxyType = await builder.BuildDtoFullProxyTypeAsync(baseDtoType, context);

            var properties = proxyType.GetProperties();

            properties.ShouldContain(p => p.Name == "Name");
            properties.ShouldContain(p => p.Name == "Description");
            properties.ShouldContain(p => p.Name == nameof(IHasFormFieldsList._formFields));

            proxyType.ShouldNotBeAssignableTo(typeof(IHasFormFieldsList));
        }

        [Fact]
        public async Task BuildDynamicDto_Test()
        {
            var entityConfigCacheMock = new Mock<IEntityConfigCache>();

            const string supervisorPropName = "Supervisor";
            const string supervisorFirstNamePropName = "FirstName";
            const string supervisorLastNamePropName = "LastName";

            entityConfigCacheMock.Setup(x => x.GetEntityPropertiesAsync(It.IsAny<Type>()))
                .Returns(() => {
                    var result = new EntityPropertyDtoList();
                    result.AddString("Name", "Name...");
                    result.AddString("Description", "Description...");

                    var nested = new EntityPropertyDto { 
                        Name = supervisorPropName,
                        DataType = DataTypes.Object,
                        Properties = new List<EntityPropertyDto>(),
                    };
                    nested.Properties.Add(new EntityPropertyDto { Name = supervisorFirstNamePropName, DataType = DataTypes.String });
                    nested.Properties.Add(new EntityPropertyDto { Name = supervisorLastNamePropName, DataType = DataTypes.String });

                    result.Add(nested);


                    var r = result as List<EntityPropertyDto>;
                    return Task.FromResult(r);
                });

            var builder = new DynamicDtoTypeBuilder(entityConfigCacheMock.Object);

            var baseDtoType = typeof(DynamicDto<Person, Guid>);

            var context = new DynamicDtoTypeBuildingContext() { ModelType = baseDtoType };
            var proxyType = await builder.BuildDtoFullProxyTypeAsync(baseDtoType, context);

            proxyType.Assembly.IsDynamic.ShouldBeTrue();

            var properties = proxyType.GetProperties();

            properties.ShouldContain(p => p.Name == "Name");
            properties.ShouldContain(p => p.Name == "Description");
            properties.ShouldContain(p => p.Name == nameof(IHasFormFieldsList._formFields));
            proxyType.ShouldNotBeAssignableTo(typeof(IHasFormFieldsList));

            var supervisorProp = properties.FirstOrDefault(p => p.Name == supervisorPropName);
            supervisorProp.ShouldNotBeNull($"{supervisorPropName} property is missing in the created DTO");

            // nested object should be dynamic
            supervisorProp.PropertyType.Assembly.IsDynamic.ShouldBeTrue();
            typeof(IDynamicNestedObject).IsAssignableFrom(supervisorProp.PropertyType).ShouldBeTrue($"Dynamic nested object must implement {nameof(IDynamicNestedObject)}");

            var nestedProperties = supervisorProp.PropertyType.GetProperties();
            nestedProperties.ShouldContain(p => p.Name == supervisorFirstNamePropName);
            nestedProperties.ShouldContain(p => p.Name == supervisorLastNamePropName);
        }
    }
}