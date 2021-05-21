using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Abp.Domain.Entities;
using Shesha.Configuration.Runtime;
using Shesha.Domain;
using Shesha.Reflection;
using Shesha.Utilities;
using EntityExtensions = Shesha.Extensions.EntityExtensions;

namespace Shesha.Web.FormsDesigner.Legacy
{
    public static class DataDrivenConfigurationProvider
    {
        static DataDrivenConfigurationProvider()
        {
            
        }

        public static ModelPropertyConfig GetHardCodedConfig(this PropertyInfo property, Type declaredType = null, bool useCamelCase = true)
        {
            var tryDeclaredType = declaredType != null && property.DeclaringType != declaredType; // here may be different classes for example when PropertyGrid is rendered using interface
            var declaredProperty = tryDeclaredType
                                    ? declaredType.GetProperty(property.Name)
                                    : null;

            var path = useCamelCase
                ? property.Name.ToCamelCase()
                : property.Name;

            var entityConfig = property.DeclaringType != null && typeof(IEntity).IsAssignableFrom(property.DeclaringType)
                ? property.DeclaringType.GetEntityConfiguration()
                : null;
            var epc = entityConfig?[property.Name];

            var result = new ModelPropertyConfig
            {
                Path = path,
                Label = ReflectionHelper.GetDisplayName(declaredProperty ?? property),
                Description = ReflectionHelper.GetDescription(declaredProperty ?? property),
                IsVisible = property.GetAttribute<BrowsableAttribute>()?.Browsable ?? true,
                Required = property.HasAttribute<RequiredAttribute>(),
                Readonly = property.GetAttribute<ReadOnlyAttribute>()?.IsReadOnly ?? false,
                DataType = epc?.GeneralType ?? GetGeneralDataType(property.DeclaringType, property.Name),
                EntityTypeShortAlias = typeof(IEntity).IsAssignableFrom(property.PropertyType)
                    ? property.PropertyType.GetEntityConfiguration()?.SafeTypeShortAlias
                    : null,
                ReferenceListName = epc?.ReferenceListName,
                ReferenceListNamespace = epc?.ReferenceListNamespace,
                EnumType = epc?.EnumType,
                OrderIndex = property.GetAttribute<DisplayAttribute>()?.GetOrder() ?? -1,
                ConfigurableByUser = property.GetAttribute<BindableAttribute>()?.Bindable ?? true,
                //GroupName = ReflectionHelper.get(declaredProperty ?? property),
            };

            return result;
        }

        public static GeneralDataType GetGeneralDataType(Type containerType, string propertyName)
        {
            return typeof(IEntity).IsAssignableFrom(containerType)
                    ? EntityExtensions.GetGeneralPropertyType(containerType, propertyName)
                    : EntityConfigurationLoaderByReflection.GetGeneralDataType(containerType.GetProperty(propertyName));
        }

        /// <summary>
        /// Validates single property
        /// </summary>
        /// <typeparam name="T">Type of model</typeparam>
        /// <param name="model">Model instance</param>
        /// <param name="propConfig">property config</param>
        /// <param name="errors">list of errors</param>
        private static void ValidateProperty<T>(T model, ModelPropertyConfig propConfig, List<ValidationError> errors)
        {
            var prop = ReflectionHelper.GetProperty(typeof(T), propConfig.Path);
            if (prop != null)
            {
                var propValue = prop.GetValue(model);
                if (propConfig.Required && (propValue == null || (propValue is string s) && string.IsNullOrWhiteSpace(s)))
                    errors.Add(new ValidationError(propConfig.Path, $"{propConfig.Label} is mandatory"));

                if (propConfig.IsEmail && !string.IsNullOrWhiteSpace(propValue?.ToString()) && !propValue.ToString().IsValidEmail())
                    errors.Add(new ValidationError(propConfig.Path, $"{propConfig.Label} is not valid email address"));

                if (propConfig.Min.HasValue && !ValidateMin(propValue, propConfig.Min.Value))
                    errors.Add(new ValidationError(propConfig.Path, $"The field {propConfig.Label} must be greater than or equal to {propConfig.Min.Value}"));

                if (propConfig.Max.HasValue && !ValidateMax(propValue, propConfig.Max.Value))
                    errors.Add(new ValidationError(propConfig.Path, $"The field {propConfig.Label} must be less than or equal to {propConfig.Max.Value}"));

                if (propConfig.MinLength.HasValue && !ValidateMinLength(propValue, propConfig.MinLength.Value))
                    errors.Add(new ValidationError(propConfig.Path, $"The field {propConfig.Label} must be a string with a minimum length of '{propConfig.MinLength}'"));

                if (propConfig.MaxLength.HasValue && !ValidateMaxLength(propValue, propConfig.MaxLength.Value))
                    errors.Add(new ValidationError(propConfig.Path, $"The field {propConfig.Label} must be a string with a maximum length of '{propConfig.MaxLength}'"));
            }
        }

        #region Validators

        public static bool ValidateMin(object value, double minValue)
        {
            if (value == null) return true;

            var isDouble = double.TryParse(Convert.ToString(value), out var valueAsDouble);

            return isDouble && valueAsDouble >= minValue;
        }

        public static bool ValidateMax(object value, double maxValue)
        {
            if (value == null) return true;

            var isDouble = double.TryParse(Convert.ToString(value), out var valueAsDouble);

            return isDouble && valueAsDouble <= maxValue;
        }

        public static bool ValidateMinLength(object value, int minLength)
        {
            var length = 0;
            // Automatically pass if value is null. RequiredAttribute should be used to assert a value is not null.
            if (value == null)
            {
                return true;
            }
            else
            {
                if (value is string str)
                {
                    length = str.Length;
                }
                else
                {
                    // We expect a cast exception if a non-{string|array} property was passed in.
                    length = ((Array)value).Length;
                }
            }

            return length >= minLength;
        }

        public static bool ValidateMaxLength(object value, int maxLength)
        {
            var length = 0;
            if (value == null)
            {
                return true;
            }
            else
            {
                var str = value as string;
                if (str != null)
                {
                    length = str.Length;
                }
                else
                {
                    // We expect a cast exception if a non-{string|array} property was passed in.
                    length = ((Array)value).Length;
                }
            }

            return length <= maxLength;
        }

        #endregion
    }

}
