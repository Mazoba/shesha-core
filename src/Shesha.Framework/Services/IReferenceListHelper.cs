using System;
using System.Collections.Generic;
using Shesha.Domain;
using Shesha.Services.ReferenceLists.Dto;

namespace Shesha.Services
{
    /// <summary>
    /// ReferenceList helper
    /// </summary>
    public interface IReferenceListHelper
    {
        /// <summary>
        /// Returns display name of the <see cref="ReferenceListItem"/> in the specified list
        /// </summary>
        /// <param name="refListNamespace">Namespace of the <see cref="ReferenceList"/></param>
        /// <param name="refListName">Name of the <see cref="ReferenceList"/></param>
        /// <param name="value">Value of the <see cref="ReferenceListItem"/></param>
        /// <returns></returns>
        string GetItemDisplayText(string refListNamespace, string refListName, Int64? value);

        /// <summary>
        /// Decompose <paramref name="value"/> into list of items. Is used for MultiValueReferenceLists
        /// </summary>
        /// <param name="refListNamespace">Namespace of the reference list</param>
        /// <param name="refListName">Name of the reference list</param>
        /// <param name="value">Value</param>
        /// <returns></returns>
        List<ReferenceListItemDto> DecomposeMultiValueIntoItems(string refListNamespace, string refListName, Int64? value);
    }
}
