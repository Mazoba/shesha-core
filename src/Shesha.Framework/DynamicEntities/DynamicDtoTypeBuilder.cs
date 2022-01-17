using Abp.Dependency;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using NHibernate.Linq;
using Shesha.Domain;
using Shesha.DynamicEntities.Dtos;
using Shesha.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace Shesha.DynamicEntities
{
    public class DynamicDtoTypeBuilder: IDynamicDtoTypeBuilder, ITransientDependency
    {
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly IRepository<EntityProperty, Guid> _propertyRepository;

        public DynamicDtoTypeBuilder(IUnitOfWorkManager unitOfWorkManager, IRepository<EntityProperty, Guid> propertyRepository)
        {
            _unitOfWorkManager = unitOfWorkManager;
            _propertyRepository = propertyRepository;
        }

        public async Task<Type> BuildDtoProxyTypeAsync(Type baseType) 
        { 
            return await CompileResultTypeAsync(baseType);
        }

        public async Task<object> CreateDtoInstanceAsync(Type baseType)
        {
            var dynamicType = await CompileResultTypeAsync(baseType);
            var dynamicObject = Activator.CreateInstance(dynamicType);
            return dynamicObject;
        }

        private async Task<List<DynamicProperty>> GetDynamicPropertiesAsync(Type type)
        {
            var entityType = DynamicDtoExtensions.GetDynamicDtoEntityType(type);
            if (entityType == null)
                throw new Exception("Failed to extract entity type of the dynamic DTO");

            var properties = new DynamicPropertyList();

            var hardCodedDtoProperties = type.GetProperties().Select(p => p.Name.ToLower()).ToList();

            using (var uow = _unitOfWorkManager.Begin()) 
            {
                var configredProperties = await _propertyRepository.GetAll().Where(p => p.EntityConfig.ClassName == entityType.Name && p.EntityConfig.Namespace == entityType.Namespace).ToListAsync();

                foreach (var property in configredProperties) 
                {
                    // skip property if already included into the DTO (hardcoded)
                    if (hardCodedDtoProperties.Contains(property.Name.ToLower()))
                        continue;

                    properties.Add(property.Name, GetDtoPropertyType(property.DataType, property.DataFormat));
                }

                await uow.CompleteAsync();
            }

            // internal fields
            properties.Add("_formFields", typeof(List<string>));

            return properties;
        }

        /// <summary>
        /// Returns .Net type that is used to store data for the specified DTO property (according to the <paramref name="dataType"/> and <paramref name="dataFormat"/>)
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="dataFormat"></param>
        /// <returns></returns>
        private static Type GetDtoPropertyType(string dataType, string dataFormat)
        {
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
                    return typeof(object); // todo: review
                case DataTypes.Array:
                    return typeof(object); // todo: review
                default:
                    throw new NotSupportedException($"Data type not supported: {dataType}");
            }
        }

        private async Task<Type> CompileResultTypeAsync(Type baseType)
        {
            var proxyClassName = $"{baseType.Name}Proxy";

            var tb = GetTypeBuilder(baseType, "DynamicModule", proxyClassName);
            var constructor = tb.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            var properties = await GetDynamicPropertiesAsync(baseType);

            foreach (var property in properties)
                CreateProperty(tb, property.PropertyName, property.PropertyType);

            var objectType = tb.CreateType();
            return objectType;
        }

        private static TypeBuilder GetTypeBuilder(Type baseType, string moduleName, string typeName)
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
                    new Type[] { typeof(IDynamicDtoProxy) });
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
    }
}
