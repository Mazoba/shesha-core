using System;

namespace Shesha.Domain.Attributes
{
    /// <summary>
    /// Attribute used to decorate any domain object property which may store
    /// zero to many values from a specified Reference List.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class MultiValueReferenceListAttribute : Attribute
    {
        public MultiValueReferenceListAttribute(string Namespace, string referenceListName)
        {
            this.Namespace = Namespace;
            ReferenceListName = referenceListName;
        }

        /// <summary>
        /// Returns <see cref="ReferenceListIdentifier"/> with current name and namespace
        /// </summary>
        public ReferenceListIdentifier GetReferenceListIdentifier()
        {
            return new ReferenceListIdentifier()
            {
                Namespace = Namespace,
                Name = ReferenceListName
            };
        }

        #region Properties
        public string Namespace { get; set; }
        public string ReferenceListName { get; set; }
        #endregion

        #region Public Functions
        /// <summary>
        /// If the property that this attribute is set on then see if it is of type int? or int which indicates
        /// that its value is a bitflag
        /// </summary>
        /// <param name="propertyInfo"></param>
        /// <returns></returns>
        public static bool IsBitFlagProperty(System.Reflection.PropertyInfo propertyInfo)
        {
            return (propertyInfo.PropertyType == typeof(System.Nullable<uint>)) || (propertyInfo.PropertyType == typeof(int));
        }

        /// <summary>
        /// Convert a bitflag value to a pipe delimited set of constituent values
        /// </summary>
        /// <param name="bitFlagValue">A nullable int bitflagValue</param>
        /// <returns></returns>
        public static string ExtractBitFlagValues(uint? bitFlagValue)
        {
            string result = string.Empty;
            if (bitFlagValue.HasValue)
            {
                if (bitFlagValue.Value == 0)
                {
                    result = "0";
                }
                else
                {
                    for (int constituentValue = 0; constituentValue <= bitFlagValue.Value; constituentValue++)
                    {
                        uint binaryValue = (uint)Math.Pow(2.0, constituentValue);
                        if (binaryValue > bitFlagValue)
                        {
                            break;
                        }
                        if ((bitFlagValue.Value | binaryValue) == bitFlagValue.Value)
                        {
                            result += binaryValue + "|";
                        }
                    }
                    if (result.EndsWith("|"))
                    {
                        result = result.Substring(0, result.Length - 1);
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// Convert a bitflag value to a pipe delimited set of constituent values
        /// </summary>
        /// <param name="bitFlagValue">bitflagValue</param>
        /// <returns></returns>
        public static string ExtractBitFlagValues(uint bitFlagValue)
        {
            string result = string.Empty;
            if (bitFlagValue == 0)
            {
                result = "0";
            }
            else
            {
                uint? convertedType = bitFlagValue;
                result = ExtractBitFlagValues(convertedType);
            }
            return result;
        }
        #endregion
    }
}
