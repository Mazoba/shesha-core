using System;

namespace Shesha.Domain.Attributes
{
    /// <summary>
    /// Attribute used to decorate any domain object property whose values
    /// should be restricted to the values of a Reference List.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Enum, AllowMultiple = false, Inherited = true)]
    public class ReferenceListAttribute : Attribute//, IMetadataAware
    {
        public ReferenceListAttribute(string referenceListNamespace, string referenceListName, string parentListItemProperty)
        {
            this.Namespace = referenceListNamespace;
            ReferenceListName = referenceListName;
            ParentListItemProperty = parentListItemProperty;
        }

        public ReferenceListAttribute(string Namespace, string referenceListName)
            : this(Namespace, referenceListName, null)
        { }

        public string Namespace { get; set; }
        public string ReferenceListName { get; set; }
        public bool OrderByName { get; set; }

        /// <summary>
        /// If the property the attribute is applied identifies a sub-reference list item to another Reference
        /// List property, this indicates the name of the parent property name.
        /// </summary>
        public string ParentListItemProperty { get; set; }

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

        #region IMetadataAware Members

        /*
        public void OnMetadataCreated(ModelMetadata metadata)
        {
            var sheshaMetadata = metadata.Shesha();

            if (sheshaMetadata.GeneralType != GeneralDataType.MultiValueReferenceList)
                sheshaMetadata.GeneralType = GeneralDataType.ReferenceList;

            sheshaMetadata.ReferenceListName = ReferenceListName;
            sheshaMetadata.ReferenceListNamespace = Namespace;
            sheshaMetadata.ReferenceListOrderByName = OrderByName;

            if (string.IsNullOrWhiteSpace(metadata.TemplateHint))
                metadata.TemplateHint = "ReferenceList";
        }
        */
        #endregion
    }
}
