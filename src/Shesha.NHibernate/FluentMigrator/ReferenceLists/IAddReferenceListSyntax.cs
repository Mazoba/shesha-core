using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shesha.FluentMigrator.ReferenceLists
{
    public interface IAddReferenceListSyntax
    {
        IAddReferenceListSyntax Description(string description);

        /// <summary>
        /// Add item
        /// </summary>
        /// <param name="value">Item value</param>
        /// <param name="item">Item text</param>
        /// <param name="orderIndex">Order index</param>
        /// <param name="description">Description</param>
        /// <returns></returns>
        IAddReferenceListSyntax AddItem(long value, string item, Int64? orderIndex = null, string description = null);
    }
}
