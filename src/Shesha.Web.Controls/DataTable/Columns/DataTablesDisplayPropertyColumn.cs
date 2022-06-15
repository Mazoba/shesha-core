using System;
using System.Reflection;
using System.Threading.Tasks;
using Abp.Domain.Entities;
using NHibernate.Util;
using Shesha.AutoMapper.Dto;
using Shesha.Domain;
using Shesha.DynamicEntities;
using Shesha.Extensions;
using Shesha.Reflection;
using Shesha.Services;
using EntityExtensions = Shesha.Extensions.EntityExtensions;

namespace Shesha.Web.DataTable.Columns
{
    /// <summary>
    /// Display property column
    /// </summary>
    public class DataTablesDisplayPropertyColumn : DataTableColumn
    {
        /// inheritedDoc
        public override async Task<object> CellContentAsync<TRow, TId>(TRow entity, bool isExport)
        {
            switch (DataType)
            {
                case ColumnDataTypes.DateTime:
                case ColumnDataTypes.Date:
                case ColumnDataTypes.Time:
                case ColumnDataTypes.Boolean:
                    {
                        var value = ReflectionHelper.GetPropertyValue(entity, PropertyName, out object parentEntity, out var propInfo);
                        return value;
                    }
                case ColumnDataTypes.MultiValueReferenceList:
                    return entity.GetMultiValueReferenceListItemNames(PropertyName, "");
                default:
                    return string.IsNullOrEmpty(PropertyName)
                        ? ""
                        : await GetPropertyValueAsync<TRow, TId>(entity, PropertyName, isExport);
            }
        }

        private async Task<object> GetPropertyValueAsync<TRow, TId>(TRow entity, string propertyName, bool isExport, string defaultValue = "") where TRow : class, IEntity<TId>
        {
            try
            {
                PropertyInfo propInfo = null;
                object parentEntity = null;

                var val = IsDynamic
                    ? await DynamicPropertyManager.Value.GetEntityPropertyAsync<TRow, TId>(entity, propertyName) 
                    : ReflectionHelper.GetPropertyValue(entity, propertyName, out parentEntity, out propInfo);
                if (val == null)
                    return defaultValue;

                var valueType = val.GetType();

                if (valueType.IsEnum) 
                {
                    var itemValue = Convert.ToInt64(val);

                    var enumType = valueType;
                    if (enumType.IsNullable())
                        enumType = Nullable.GetUnderlyingType(enumType);

                    return ReflectionHelper.GetEnumDescription(enumType, itemValue);
                }

                if (valueType.IsEntityType()) 
                {
                    var displayProperty = valueType.GetEntityConfiguration().DisplayNamePropertyInfo;
                    var displayText = displayProperty != null
                        ? displayProperty.GetValue(val)?.ToString()
                        : val.ToString();

                    if (DataTableConfig != null && !DataTableConfig.UseDtos || isExport)
                        return displayText;

                    var dto = new EntityWithDisplayNameDto<string>(val.GetId().ToString(), displayText);
                    return dto;
                }


                var propConfig = parentEntity != null && propInfo != null
                    ? parentEntity.GetType().GetEntityConfiguration()[propInfo.Name]
                    : null;

                if (propConfig != null)
                {
                    if (propConfig.GeneralType == Configuration.Runtime.GeneralDataType.ReferenceList) 
                    {
                        var refListHelper = StaticContext.IocManager.Resolve<IReferenceListHelper>();
                        var itemValue = Convert.ToInt64(val);
                        var displayText = refListHelper.GetItemDisplayText(propConfig.ReferenceListNamespace, propConfig.ReferenceListName, itemValue);

                        if (DataTableConfig != null && !DataTableConfig.UseDtos || isExport)
                            return displayText;

                        var dto = new ReferenceListItemValueDto
                        {
                            Item = displayText,
                            ItemValue = itemValue
                        };
                        return dto;
                    }
                    else
                        return EntityExtensions.GetPrimitiveTypePropertyDisplayText(val, propInfo, defaultValue);
                }

                return val;
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"An error occured whilst trying to retrieve DisplayText of property '{propertyName}' on type of '{entity.GetType().FullName}'.", ex);
            }
        }

        protected Lazy<IDynamicPropertyManager> DynamicPropertyManager = new Lazy<IDynamicPropertyManager>(() => StaticContext.IocManager.Resolve<IDynamicPropertyManager>());

        internal DataTablesDisplayPropertyColumn() : base()
        {
        }
    }
}
