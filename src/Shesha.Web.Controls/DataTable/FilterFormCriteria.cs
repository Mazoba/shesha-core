using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using Shesha.Configuration.Runtime;
using Shesha.Extensions;

namespace Shesha.Web.DataTable
{
    /// <summary>
    /// Represents Filtering Criteria passed in from a Web Form.
    /// </summary>
    public class FilterFormCriteria
    {
        #region Contructors and Initialisation

        public FilterFormCriteria()
        {
            Init();
        }

        public FilterFormCriteria(Type filteredType)
        {
            Init();

            FilteredType = filteredType;
        }

        private void Init()
        {
            Criteria = new List<Criterion>();
        }

        #endregion

        public Type FilteredType { get; set; }

        public IList<Criterion> Criteria { get; protected set; }

        public class Criterion
        {
            public PropertyInfo PropInfo { get; protected set; }
            public FilterOperator FilterOperator { get; protected set; }
            public string Name { get; protected set; }
            public object Value { get; protected set; }

            public Criterion(PropertyInfo propInfo, FilterOperator @operator, object value, string name)
            {
                Name = name;
                Value = value;
                PropInfo = propInfo;
                FilterOperator = @operator;

                if (propInfo != null)
                {
                    var validOps = FilterFormCriteria.GetValidFilterOperators(propInfo.GetGeneralDataType());
                    if (!validOps.Contains(@operator))
                        throw new Exception("Specified operator is not valid for a property of the specified type.");
                }
            }

        }

        public NameValueCollection FormValues { get; internal set; }

        public static FilterOperator[] GetValidFilterOperators(GeneralDataType generalType)
        {
            var res = new List<FilterOperator>();
            switch (generalType)
            {
                case GeneralDataType.Date:
                case GeneralDataType.Time:
                case GeneralDataType.DateTime:
                case GeneralDataType.Numeric:
                    return new FilterOperator[] {
                        FilterOperator.EqualTo,
                        FilterOperator.GreaterOrEqualTo,
                        FilterOperator.GreaterThan,
                        FilterOperator.LessOrEqualTo,
                        FilterOperator.LessThan
                    };
                case GeneralDataType.Text:
                    return new FilterOperator[] {
                        FilterOperator.EqualTo,
                        FilterOperator.Like};
                case GeneralDataType.Enum:
                    return new FilterOperator[] {
                        FilterOperator.EqualTo
                    };
                case GeneralDataType.ReferenceList:
                    return new FilterOperator[] {
                        FilterOperator.In,
                        FilterOperator.Any,
                        FilterOperator.EqualTo
                    };
                case GeneralDataType.MultiValueReferenceList:
                    return new FilterOperator[] {
                        FilterOperator.Any,
                        FilterOperator.All
                    };
                case GeneralDataType.EntityReference:
                case GeneralDataType.Boolean:
                    return new FilterOperator[] {
                        FilterOperator.EqualTo};
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    /// <summary>
    /// todo: merge with RefListFilterComparerType
    /// </summary>
    public enum FilterOperator
    {
        GreaterThan,
        LessThan,
        EqualTo,
        GreaterOrEqualTo,
        LessOrEqualTo,

        Like,
        All,
        Any,
        In
    }
}
