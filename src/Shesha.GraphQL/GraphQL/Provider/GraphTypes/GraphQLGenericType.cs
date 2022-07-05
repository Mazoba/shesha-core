﻿using Abp.Domain.Uow;
using GraphQL;
using GraphQL.Types;
using Shesha.DynamicEntities;
using Shesha.DynamicEntities.Cache;
using Shesha.Extensions;
using Shesha.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shesha.GraphQL.Provider.GraphTypes
{
    /// <summary>
    /// https://github.com/fenomeno83/graphql-dotnet-auto-types
    /// </summary>
    public class GraphQLGenericType<TModel> : ObjectGraphType<TModel> where TModel : class
    {
        private readonly IDynamicPropertyManager _dynamicPropertyManager;
        private readonly IEntityConfigCache _entityConfigCache;
        private readonly IDynamicDtoTypeBuilder _dynamicDtoTypeBuilder;
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        public GraphQLGenericType(IDynamicPropertyManager dynamicPropertyManager, IEntityConfigCache entityConfigCache, IDynamicDtoTypeBuilder dynamicDtoTypeBuilder, IUnitOfWorkManager unitOfWorkManager)
        {
            _dynamicPropertyManager = dynamicPropertyManager;
            _entityConfigCache = entityConfigCache;
            _dynamicDtoTypeBuilder = dynamicDtoTypeBuilder;
            _unitOfWorkManager = unitOfWorkManager;

            var genericType = typeof(TModel);

            Name = MakeName(typeof(TModel));

            var propsInfo = genericType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            if (propsInfo == null || propsInfo.Length == 0)
                throw new GraphQLSchemaException(genericType.Name, $"Unable to create generic GraphQL type from type {genericType.Name} because it has no properties");

            var properties = genericType.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();
            foreach (var property in properties) 
            {
                EmitField(property);
                
                // for nested entities add raw Id property
                if (property.PropertyType.IsEntityType()) 
                {
                    var nestedEntityIdName = $"{property.Name}Id";

                    // skip if property already declared
                    if (!properties.Any(p => p.Name.Equals(nestedEntityIdName, StringComparison.InvariantCultureIgnoreCase))) 
                    {
                        var idType = property.PropertyType.GetEntityIdType();

                        Field(GraphTypeMapper.GetGraphType(idType, isInput: false), nestedEntityIdName, $"Id of the {property.Name}",
                            resolve: context => {
                                var nestedEntity = property.GetValue(context.Source);
                                return nestedEntity?.GetId();
                            }
                        );
                    }
                }
            }

            // add dynamic properties
            if (typeof(TModel).IsEntityType()) 
            {
                AsyncHelper.RunSync(async () => {
                    using (var uow = _unitOfWorkManager.Begin())
                    {
                        var properties = await _entityConfigCache.GetEntityPropertiesAsync(typeof(TModel));
                        var dynamicProps = properties.Where(p => p.Source == Domain.Enums.MetadataSourceType.UserDefined).ToList();
                        foreach (var dynamicProp in dynamicProps)
                        {
                            var propType = await _dynamicDtoTypeBuilder.GetDtoPropertyTypeAsync(dynamicProp, new DynamicDtoTypeBuildingContext());
                            FieldAsync(GraphTypeMapper.GetGraphType(propType, isInput: false), dynamicProp.Name, dynamicProp.Description,
                                resolve: async context => {
                                    try
                                    {
                                        var value = await _dynamicPropertyManager.GetPropertyAsync(context.Source, dynamicProp.Name);
                                        return value;
                                    }
                                    catch (Exception e)
                                    {
                                        throw;
                                    }
                                }
                            );
                        }
                        await uow.CompleteAsync();
                    }
                });
            }
        }

        private static string MakeName(Type type)
        {
            if (type.IsAssignableToGenericType(typeof(IDictionary<,>)))
            {
                return "Dictionary";
            }

            return type.GetNamedType().Name;
        }

        private void EmitField(PropertyInfo propertyInfo)
        {
            var isDictionary = propertyInfo.PropertyType.IsAssignableToGenericType(typeof(IDictionary<,>));
            var typeName = propertyInfo.PropertyType.Name;
            if (isDictionary || propertyInfo.PropertyType.Namespace != null && !propertyInfo.PropertyType.Namespace.StartsWith("System"))
            {
                if (propertyInfo.PropertyType.IsEnum)
                    Field(GraphTypeMapper.GetGraphType(propertyInfo.PropertyType, isInput: false), propertyInfo.Name, resolve: context => Convert.ToInt32(propertyInfo.GetValue(context.Source)));
                else
                {
                    var gqlType = Assembly.GetAssembly(typeof(ISchema)).GetTypes().FirstOrDefault(t => t.Name == $"{typeName}Type" && t.IsAssignableTo(typeof(IGraphType)));
                    /*
                    gqlType ??= isDictionary
                        ? propertyInfo.PropertyType.IsAssignableTo(typeof(ExtraPropertyDictionary))
                            ? typeof(AbpExtraPropertyGraphType)
                            : MakeDictionaryType(propertyInfo)
                        : typeof(GraphQLGenericType<>).MakeGenericType(propertyInfo.PropertyType);
                    */
                    gqlType ??= isDictionary
                        ? MakeDictionaryType(propertyInfo)
                        : typeof(GraphQLGenericType<>).MakeGenericType(propertyInfo.PropertyType);

                    Field(gqlType, propertyInfo.Name);
                }
            }
            else
            {
                switch (typeName)
                {
                    case "List`1":
                    case "IList`1":
                    case "ICollection`1":
                        {
                            var gtn = propertyInfo.PropertyType.GetGenericArguments().First();
                            var gqlListType = GraphTypeMapper.GetGraphType(gtn, isInput: false);
                            var listType = typeof(ListGraphType<>).MakeGenericType(gqlListType);
                            Field(listType, propertyInfo.Name);
                            break;
                        }
                    case nameof(Boolean): Field(GraphTypeMapper.GetGraphType(propertyInfo.PropertyType, isInput: false), propertyInfo.Name); break;
                    case nameof(Int32): Field(GraphTypeMapper.GetGraphType(propertyInfo.PropertyType, isInput: false), propertyInfo.Name); break;
                    case nameof(Int64): Field(GraphTypeMapper.GetGraphType(propertyInfo.PropertyType, isInput: false), propertyInfo.Name); break;
                    case nameof(Int16): Field(GraphTypeMapper.GetGraphType(propertyInfo.PropertyType, isInput: false), propertyInfo.Name); break;
                    case nameof(Single): Field(GraphTypeMapper.GetGraphType(propertyInfo.PropertyType, isInput: false), propertyInfo.Name); break;
                    case nameof(Double): Field(GraphTypeMapper.GetGraphType(propertyInfo.PropertyType, isInput: false), propertyInfo.Name); break;
                    case nameof(Decimal): Field(GraphTypeMapper.GetGraphType(propertyInfo.PropertyType, isInput: false), propertyInfo.Name); break;
                    case nameof(Byte): Field(GraphTypeMapper.GetGraphType(propertyInfo.PropertyType, isInput: false), propertyInfo.Name, resolve: context => Convert.ToInt32(propertyInfo.GetValue(context.Source))); break;
                    case nameof(DateTime): Field(GraphTypeMapper.GetGraphType(propertyInfo.PropertyType, isInput: false), propertyInfo.Name); break;
                    case "Nullable`1":
                        {
                            var underlyingType = Nullable.GetUnderlyingType(propertyInfo.PropertyType);
                            if (underlyingType.IsEnum)
                            {

                                Field(GraphTypeMapper.GetGraphType(underlyingType, isInput: false), propertyInfo.Name, resolve: context =>
                                {
                                    var nullableEnum = propertyInfo.GetValue(context.Source);
                                    if (nullableEnum != null) return (int)nullableEnum;
                                    else return null;
                                });
                            }
                            else
                            {
                                switch (underlyingType.Name)
                                {
                                    case nameof(Int32):
                                        Field(GraphTypeMapper.GetGraphType(underlyingType, isInput: false), propertyInfo.Name, resolve: context =>
                                        {
                                            var nullableInt = propertyInfo.GetValue(context.Source) as int?;
                                            if (nullableInt.HasValue) return nullableInt.Value;
                                            else return null;
                                        }); break;
                                    case nameof(Byte):
                                        Field(GraphTypeMapper.GetGraphType(underlyingType, isInput: false), propertyInfo.Name, resolve: context =>
                                        {
                                            var nullableByte = propertyInfo.GetValue(context.Source) as byte?;
                                            if (nullableByte.HasValue) return nullableByte.Value;
                                            else return null;
                                        }); break;
                                    case nameof(Int16):
                                        Field(GraphTypeMapper.GetGraphType(underlyingType, isInput: false), propertyInfo.Name, resolve: context =>
                                        {
                                            var nullableShort = propertyInfo.GetValue(context.Source) as short?;
                                            if (nullableShort.HasValue) return nullableShort.Value;
                                            else return null;
                                        }); break;
                                    case nameof(Int64):
                                        Field(GraphTypeMapper.GetGraphType(underlyingType, isInput: false), propertyInfo.Name, resolve: context =>
                                        {
                                            var nullableLong = propertyInfo.GetValue(context.Source) as long?;
                                            if (nullableLong.HasValue) return nullableLong.Value;
                                            else return null;
                                        }); break;
                                    case nameof(Double):
                                        Field(GraphTypeMapper.GetGraphType(underlyingType, isInput: false), propertyInfo.Name, resolve: context =>
                                        {
                                            var nullableDouble = propertyInfo.GetValue(context.Source) as double?;
                                            if (nullableDouble.HasValue) return nullableDouble.Value;
                                            else return null;
                                        }); break;
                                    case nameof(Single):
                                        Field(GraphTypeMapper.GetGraphType(underlyingType, isInput: false), propertyInfo.Name, resolve: context =>
                                        {
                                            var nullableSingle = propertyInfo.GetValue(context.Source) as float?;
                                            if (nullableSingle.HasValue) return nullableSingle.Value;
                                            else return null;
                                        }); break;
                                    case nameof(Boolean):
                                        Field(GraphTypeMapper.GetGraphType(underlyingType, isInput: false), propertyInfo.Name, resolve: context =>
                                        {
                                            var nullableBoolean = propertyInfo.GetValue(context.Source) as bool?;
                                            if (nullableBoolean.HasValue) return nullableBoolean.Value;
                                            else return null;
                                        }); break;
                                    case nameof(Decimal):
                                        Field(GraphTypeMapper.GetGraphType(underlyingType, isInput: false), propertyInfo.Name, resolve: context =>
                                        {
                                            var nullableDecimal = propertyInfo.GetValue(context.Source) as decimal?;
                                            if (nullableDecimal.HasValue) return nullableDecimal.Value;
                                            else return null;
                                        }); break;
                                    case nameof(DateTime):
                                        Field(GraphTypeMapper.GetGraphType(underlyingType, isInput: false), propertyInfo.Name, resolve: context =>
                                        {
                                            var nullableDateTime = propertyInfo.GetValue(context.Source) as DateTime?;
                                            if (nullableDateTime.HasValue) return nullableDateTime.Value;
                                            else return null;
                                        }); break;
                                }
                            }
                        }
                        break;
                    case nameof(String):
                    default: 
                        {
                            Field(GraphTypeMapper.GetGraphType(propertyInfo.PropertyType, isInput: false), propertyInfo.Name); 
                            break;
                        } 
                }
            }
        }

        private Type MakeDictionaryType(PropertyInfo propertyInfo)
        {
            var dictType = propertyInfo.PropertyType.GetGenericTypeAssignableTo(typeof(IDictionary<,>));

            var args = dictType.GetGenericArguments();

            return typeof(DictionaryGraphType<,>).MakeGenericType(args[0], args[1]);
        }
    }
}
