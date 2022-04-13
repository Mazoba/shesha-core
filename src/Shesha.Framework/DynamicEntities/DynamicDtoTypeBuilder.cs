﻿using Abp.Dependency;
using Abp.Events.Bus.Entities;
using Abp.Events.Bus.Handlers;
using Abp.Runtime.Caching;
using Castle.Core.Logging;
using Shesha.AutoMapper.Dto;
using Shesha.Configuration.Runtime;
using Shesha.Domain;
using Shesha.DynamicEntities.Cache;
using Shesha.DynamicEntities.Dtos;
using Shesha.DynamicEntities.Json;
using Shesha.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Shesha.DynamicEntities
{
    public class DynamicDtoTypeBuilder : IEventHandler<EntityChangedEventData<EntityProperty>>, IDynamicDtoTypeBuilder, ITransientDependency
    {
        private readonly IEntityConfigCache _entityConfigCache;
        private IEntityConfigurationStore _entityConfigurationStore;
        private readonly ICacheManager _cacheManager;

        /// <summary>
        /// Cache of the ReferenceListItems
        /// </summary>
        protected ITypedCache<string, DynamicDtoProxyCacheItem> FullProxyCache => _cacheManager.GetCache<string, DynamicDtoProxyCacheItem>("DynamicDtoTypeBuilder_FullProxyCache");

        /// <summary>
        /// Reference to the logger to write logs.
        /// </summary>
        public ILogger Logger { protected get; set; } = NullLogger.Instance;

        public DynamicDtoTypeBuilder(IEntityConfigCache entityConfigCache, IEntityConfigurationStore entityConfigurationStore, ICacheManager cacheManager)
        {
            _entityConfigCache = entityConfigCache;
            _entityConfigurationStore = entityConfigurationStore;
            _cacheManager = cacheManager;
        }

        /// inheritedDoc
        public async Task<Type> BuildDtoProxyTypeAsync(DynamicDtoTypeBuildingContext context)
        {
            return await CompileResultTypeAsync(context);
        }

        public async Task<List<EntityPropertyDto>> GetEntityPropertiesAsync(Type entityType)
        {
            return await _entityConfigCache.GetEntityPropertiesAsync(entityType);
        }

        public async Task<List<DynamicProperty>> GetDynamicPropertiesAsync(Type type, DynamicDtoTypeBuildingContext context)
        {
            var entityType = DynamicDtoExtensions.GetDynamicDtoEntityType(type);
            if (entityType == null)
                throw new Exception("Failed to extract entity type of the dynamic DTO");

            var properties = new DynamicPropertyList();

            var hardCodedDtoProperties = type.GetProperties().Select(p => p.Name.ToLower()).ToList();

            var configuredProperties = await GetEntityPropertiesAsync(entityType);
            foreach (var property in configuredProperties)
            {
                // skip property if already included into the DTO (hardcoded)
                if (hardCodedDtoProperties.Contains(property.Name.ToLower()))
                    continue;

                if (string.IsNullOrWhiteSpace(property.DataType))
                {
                    Logger.Warn($"Type '{type.FullName}': {nameof(property.DataType)} of property {property.Name} is empty");
                    continue;
                }

                var propertyType = await GetDtoPropertyTypeAsync(property, context);
                if (propertyType != null)
                    properties.Add(property.Name, propertyType);
            }

            // internal fields
            if (context.AddFormFieldsProperty)
                properties.Add(nameof(IHasFormFieldsList._formFields), typeof(List<string>));

            return properties;
        }

        /*
        /// <summary>
        /// Returns .Net type that is used to store data for the specified DTO property (according to the <paramref name="dataType"/> and <paramref name="dataFormat"/>)
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="dataFormat"></param>
        /// <returns></returns>
        public async Task<Type> GetDtoPropertyTypeAsync(EntityPropertyDto propertyDto) 
        {
            return await GetDtoPropertyTypeAsync(propertyDto.DataType, propertyDto.DataFormat);
        }
        */

        /// <summary>
        /// Returns .Net type that is used to store data for the specified DTO property (according to the property settings)
        /// </summary>
        public async Task<Type> GetDtoPropertyTypeAsync(EntityPropertyDto propertyDto, DynamicDtoTypeBuildingContext context)
        {
            var dataType = propertyDto.DataType;
            var dataFormat = propertyDto.DataFormat;

            switch (dataType)
            {
                case DataTypes.Guid:
                    return typeof(Guid?);
                case DataTypes.String:
                    return typeof(string);
                case DataTypes.Date:
                case DataTypes.DateTime:
                    return typeof(DateTime?);
                case DataTypes.Time:
                    return typeof(TimeSpan?);
                case DataTypes.Boolean:
                    return typeof(bool?);
                case DataTypes.ReferenceListItem:
                    // todo: find a way to check an entity property
                    // if it's declared as an enum - get base type of this enum
                    // if it's declared as int/Int64 - use this type
                    return typeof(Int64?);

                case DataTypes.Number:
                    {
                        switch (dataFormat)
                        {
                            case NumberFormats.Int32:
                                return typeof(int?);
                            case NumberFormats.Int64:
                                return typeof(Int64?);
                            case NumberFormats.Float:
                                return typeof(float?);
                            case NumberFormats.Double:
                                return typeof(decimal?);
                            default:
                                return typeof(decimal?);
                        }
                    }

                case DataTypes.EntityReference:
                    return GetEntityReferenceType(propertyDto, context);
                case DataTypes.Array: 
                {
                    if (propertyDto.ItemsType == null) 
                        return null;
                    
                    var nestedType = await GetDtoPropertyTypeAsync(propertyDto.ItemsType, context);
                    var arrayType = typeof(List<>).MakeGenericType(nestedType);
                    return arrayType;
                }
                case DataTypes.Object:
                    return await BuildNestedTypeAsync(propertyDto, context); // JSON content
                default:
                    throw new NotSupportedException($"Data type not supported: {dataType}");
            }
        }

        private Type GetEntityReferenceType(EntityPropertyDto propertyDto, DynamicDtoTypeBuildingContext context) 
        {
            if (propertyDto.DataType != DataTypes.EntityReference)
                throw new NotSupportedException($"DataType {propertyDto.DataType} is not supported. Expected {DataTypes.EntityReference}");

            if (string.IsNullOrWhiteSpace(propertyDto.EntityType))
                return null;

            var entityConfig = _entityConfigurationStore.Get(propertyDto.EntityType);
            if (entityConfig == null)
                return null;

            return context.UseDtoForEntityReferences
                ? typeof(EntityWithDisplayNameDto<>).MakeGenericType(entityConfig.IdType)
                : entityConfig?.IdType;
        }

        private async Task<Type> BuildNestedTypeAsync(EntityPropertyDto propertyDto, DynamicDtoTypeBuildingContext context) 
        {
            if (propertyDto.DataType != DataTypes.Object)
                throw new NotSupportedException($"{nameof(BuildNestedTypeAsync)}: unsupported type of property (expected '{DataTypes.Object}', actual: '{propertyDto.DataType}')");

            // todo: build name of the class according ot the level of the property
            using (context.OpenNamePrefix(propertyDto.Name)) 
            {
                var className = context.CurrentPrefix.Replace('.', '_');

                var tb = GetTypeBuilder(typeof(object), className, new List<Type> { typeof(IDynamicNestedObject) });
                var constructor = tb.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

                foreach (var property in propertyDto.Properties)
                {
                    //if (propertyFilter == null || propertyFilter.Invoke(property.PropertyName))
                    var propertyType = await GetDtoPropertyTypeAsync(property, context);
                    CreateProperty(tb, property.Name, propertyType);
                }

                var objectType = tb.CreateType();

                context.ClassCreated(objectType);

                return objectType;
            }
        }

        private async Task<Type> CompileResultTypeAsync(DynamicDtoTypeBuildingContext context)
        {
            var proxyClassName = GetProxyTypeName(context.ModelType, "Proxy");

            var tb = GetTypeBuilder(context.ModelType, proxyClassName, new List<Type> { typeof(IDynamicDtoProxy) });
            var constructor = tb.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            using (context.OpenNamePrefix(proxyClassName)) 
            {
                var properties = await GetDynamicPropertiesAsync(context.ModelType, context);

                foreach (var property in properties)
                {
                    if (context.PropertyFilter == null || context.PropertyFilter.Invoke(property.PropertyName))
                        CreateProperty(tb, property.PropertyName, property.PropertyType);
                }

                var objectType = tb.CreateType();

                context.ClassCreated(objectType);

                return objectType;
            }
        }

        private static TypeBuilder GetTypeBuilder(Type baseType, string typeName, IEnumerable<Type> interfaces)
        {
            var assemblyName = new AssemblyName($"{typeName}Assembly");
            var moduleName = $"{typeName}Module";
            
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(moduleName);
            var tb = moduleBuilder.DefineType(typeName,
                    TypeAttributes.Public |
                    TypeAttributes.Class |
                    TypeAttributes.AutoClass |
                    TypeAttributes.AnsiClass |
                    TypeAttributes.BeforeFieldInit |
                    TypeAttributes.AutoLayout,
                    baseType,
                    interfaces.ToArray());
            return tb;
        }

        private static void CreateProperty(TypeBuilder tb, string propertyName, Type propertyType)
        {
            var fieldBuilder = tb.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);

            var propertyBuilder = tb.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);
            var getPropMthdBldr = tb.DefineMethod("get_" + propertyName, 
                MethodAttributes.Public | 
                MethodAttributes.SpecialName | 
                MethodAttributes.HideBySig |
                MethodAttributes.Virtual, 
                propertyType, 
                Type.EmptyTypes);
            ILGenerator getIl = getPropMthdBldr.GetILGenerator();

            getIl.Emit(OpCodes.Ldarg_0);
            getIl.Emit(OpCodes.Ldfld, fieldBuilder);
            getIl.Emit(OpCodes.Ret);

            var setPropMthdBldr =
                tb.DefineMethod("set_" + propertyName,
                  MethodAttributes.Public |
                  MethodAttributes.SpecialName |
                  MethodAttributes.HideBySig |
                  MethodAttributes.Virtual,
                  null, new[] { propertyType });

            ILGenerator setIl = setPropMthdBldr.GetILGenerator();
            var modifyProperty = setIl.DefineLabel();
            var exitSet = setIl.DefineLabel();

            setIl.MarkLabel(modifyProperty);
            setIl.Emit(OpCodes.Ldarg_0);
            setIl.Emit(OpCodes.Ldarg_1);
            setIl.Emit(OpCodes.Stfld, fieldBuilder);

            setIl.Emit(OpCodes.Nop);
            setIl.MarkLabel(exitSet);
            setIl.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getPropMthdBldr);
            propertyBuilder.SetSetMethod(setPropMthdBldr);

            AddPropertyAttributes(propertyBuilder, propertyType);
            //propertyBuilder

            // https://stackoverflow.com/questions/1822047/how-to-emit-explicit-interface-implementation-using-reflection-emit
            // DefineMethodOverride is used to associate the method 
            // body with the interface method that is being implemented.
            //
            /*
            if (propertyName == "Id") 
            {
                var getMethod = typeof(IEntity<Guid>).GetMethod("get_Id");
                tb.DefineMethodOverride(getPropMthdBldr, getMethod);

                var setMethod = typeof(IEntity<Guid>).GetMethod("set_Id");
                tb.DefineMethodOverride(setPropMthdBldr, setMethod);
            }            
            */
        }

        private static void AddPropertyAttributes(PropertyBuilder propertyBuilder, Type propertyType)
        {
            if (propertyType == typeof(DateTime) || propertyType == typeof(DateTime?))
            {
                var attrCtorParams = new Type[] { typeof(Type) };
                var attrCtorInfo = typeof(JsonConverterAttribute).GetConstructor(attrCtorParams);
                var attrBuilder = new CustomAttributeBuilder(attrCtorInfo, new object[] { typeof(DateConverter) });
                propertyBuilder.SetCustomAttribute(attrBuilder);
            }
        }

        private string GetProxyTypeName(Type type, string suffix) 
        {
            return $"{type.Name}{suffix}";
        }

        public async Task<Type> BuildDtoFullProxyTypeAsync(Type baseType, DynamicDtoTypeBuildingContext context)
        {
            var cacheKey = GetTypeCacheKey(baseType, context);
            var cachedDtoItem = await FullProxyCache.GetOrDefaultAsync(cacheKey);
            if (cachedDtoItem != null) 
            {
                context.Classes = cachedDtoItem.NestedClasses.ToDictionary(i => i.Key, i => i.Value);
                return cachedDtoItem.DtoType;
            }                

            var proxyClassName = GetProxyTypeName(baseType, "FullProxy");
            var properties = await GetDynamicPropertiesAsync(baseType, context);
            
            if (context.AddFormFieldsProperty && !properties.Any(p => p.PropertyName == nameof(IHasFormFieldsList._formFields)))
                properties.Add(new DynamicProperty { PropertyName = nameof(IHasFormFieldsList._formFields), PropertyType = typeof(List<string>) });

            var interfaces = new List<Type>();
            if (context.AddFormFieldsProperty)
                interfaces.Add(typeof(IHasFormFieldsList));

            var type = await CompileResultTypeAsync(baseType, proxyClassName, interfaces, properties, context);

            await FullProxyCache.SetAsync(cacheKey, new DynamicDtoProxyCacheItem { 
                DtoType = type,
                NestedClasses = context.Classes.ToDictionary(i => i.Key, i => i.Value)
            });

            return type;
        }

        private async Task<Type> CompileResultTypeAsync(Type baseType,
            string proxyClassName,
            List<Type> interfaces,
            List<DynamicProperty> properties, 
            DynamicDtoTypeBuildingContext context)
        {
            var tb = GetTypeBuilder(baseType, proxyClassName, interfaces.Union(new List<Type> { typeof(IDynamicDtoProxy) }));
            var constructor = tb.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            foreach (var property in properties)
            {
                CreateProperty(tb, property.PropertyName, property.PropertyType);
            }

            var objectType = tb.CreateType();

            context.ClassCreated(objectType);

            return objectType;
        }

        private string GetTypeCacheKey(Type type, DynamicDtoTypeBuildingContext context) 
        {
            var entityType = DynamicDtoExtensions.GetDynamicDtoEntityType(type);
            if (entityType == null)
                throw new NotSupportedException($"Type '{type.FullName}' is not a dynamic DTO");

            return $"{entityType.FullName}|formFields:{context.AddFormFieldsProperty.ToString().ToLower()}|useEntityDtos:{context.UseDtoForEntityReferences.ToString().ToLower()}";
        }

        public void HandleEvent(EntityChangedEventData<EntityProperty> eventData)
        {
            var entityConfig = eventData.Entity?.EntityConfig;

            if (entityConfig == null)
                return;

            var cacheKey = $"{entityConfig.Namespace}.{entityConfig.ClassName}";

            FullProxyCache.Remove(cacheKey);
        }
    }
}
