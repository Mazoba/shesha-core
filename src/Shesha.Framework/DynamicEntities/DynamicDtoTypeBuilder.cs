using Abp.Dependency;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.ObjectMapping;
using Abp.Runtime.Caching;
using NHibernate.Linq;
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
    public class DynamicDtoTypeBuilder : IDynamicDtoTypeBuilder, ITransientDependency
    {
        private readonly IEntityConfigCache _entityConfigCache;
        
        public DynamicDtoTypeBuilder(IEntityConfigCache entityConfigCache)
        {
            _entityConfigCache = entityConfigCache;
        }

        public async Task<Type> BuildDtoProxyTypeAsync(Type baseType, Func<string, bool> propertyFilter)
        {
            return await CompileResultTypeAsync(baseType, propertyFilter);
        }

        public async Task<object> CreateDtoInstanceAsync(Type baseType)
        {
            var dynamicType = await CompileResultTypeAsync(baseType);
            var dynamicObject = Activator.CreateInstance(dynamicType);
            return dynamicObject;
        }

        public async Task<List<EntityPropertyDto>> GetEntityPropertiesAsync(Type entityType)
        {
            return await _entityConfigCache.GetEntityPropertiesAsync(entityType);
        }

        public async Task<List<DynamicProperty>> GetDynamicPropertiesAsync(Type type, string prefix = "")
        {
            var entityType = DynamicDtoExtensions.GetDynamicDtoEntityType(type);
            if (entityType == null)
                throw new Exception("Failed to extract entity type of the dynamic DTO");

            var properties = new DynamicPropertyList();

            var hardCodedDtoProperties = type.GetProperties().Select(p => p.Name.ToLower()).ToList();

            var configredProperties = await GetEntityPropertiesAsync(entityType);
            foreach (var property in configredProperties)
            {
                // skip property if already included into the DTO (hardcoded)
                if (hardCodedDtoProperties.Contains(property.Name.ToLower()))
                    continue;

                var propertyType = await GetDtoPropertyTypeAsync(property, prefix);
                if (propertyType != null)
                    properties.Add(property.Name, propertyType);
            }

            // internal fields
            properties.Add("_formFields", typeof(List<string>));

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
        public async Task<Type> GetDtoPropertyTypeAsync(EntityPropertyDto propertyDto, string prefix = "")
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
                    return null;
                case DataTypes.Array:
                    return null;
                case DataTypes.Object:
                    return await BuildNestedTypeAsync(propertyDto, prefix); // JSON content
                default:
                    throw new NotSupportedException($"Data type not supported: {dataType}");
            }
        }

        private async Task<Type> BuildNestedTypeAsync(EntityPropertyDto propertyDto, string prefix = "") 
        {
            if (propertyDto.DataType != DataTypes.Object)
                throw new NotSupportedException($"{nameof(BuildNestedTypeAsync)}: unsupported type of property (expected '{DataTypes.Object}', actual: '{propertyDto.DataType}')");

            // todo: build name of the class according ot the level of the property
            var tb = GetTypeBuilder(typeof(object), "DynamicModule", $"{prefix}_{propertyDto.Name}", new List<Type> { typeof(IDynamicNestedObject) });
            var constructor = tb.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            foreach (var property in propertyDto.Properties)
            {
                //if (propertyFilter == null || propertyFilter.Invoke(property.PropertyName))
                var propertyType = await GetDtoPropertyTypeAsync(property, prefix + propertyDto.Name);
                CreateProperty(tb, property.Name, propertyType);
            }

            var objectType = tb.CreateType();
            return objectType;
        }

        private async Task<Type> CompileResultTypeAsync(Type baseType, Func<string, bool> propertyFilter = null)
        {
            var proxyClassName = GetProxyTypeName(baseType, "Proxy");

            var tb = GetTypeBuilder(baseType, "DynamicModule", proxyClassName, new List<Type> { typeof(IDynamicDtoProxy) });
            var constructor = tb.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            var properties = await GetDynamicPropertiesAsync(baseType, proxyClassName);

            foreach (var property in properties) 
            {
                if (propertyFilter == null || propertyFilter.Invoke(property.PropertyName))
                    CreateProperty(tb, property.PropertyName, property.PropertyType);
            }

            var objectType = tb.CreateType();
            return objectType;
        }

        private static TypeBuilder GetTypeBuilder(Type baseType, string moduleName, string typeName, IEnumerable<Type> interfaces)
        {
            var an = new AssemblyName(moduleName);
            
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);
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

        public async Task<Type> BuildDtoFullProxyTypeAsync(Type baseType)
        {
            var proxyClassName = GetProxyTypeName(baseType, "FullProxy");
            var properties = await GetDynamicPropertiesAsync(baseType);
            
            if (!properties.Any(p => p.PropertyName == nameof(IHasFormFieldsList._formFields)))
                properties.Add(new DynamicProperty { PropertyName = nameof(IHasFormFieldsList._formFields), PropertyType = typeof(List<string>) });

            var type = await CompileResultTypeAsync(baseType, proxyClassName, new List<Type> { typeof(IHasFormFieldsList) }, properties);
            return type;
        }

        private async Task<Type> CompileResultTypeAsync(Type baseType,
            string proxyClassName,
            List<Type> interfaces,
            List<DynamicProperty> properties)
        {
            var tb = GetTypeBuilder(baseType, "DynamicModule", proxyClassName, interfaces.Union(new List<Type> { typeof(IDynamicDtoProxy) }));
            var constructor = tb.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            foreach (var property in properties)
            {
                CreateProperty(tb, property.PropertyName, property.PropertyType);
            }

            var objectType = tb.CreateType();
            return objectType;
        }
    }
}
