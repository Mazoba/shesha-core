using System;

namespace Shesha.FluentMigrator.ReferenceLists
{
    public interface IUpdateReferenceListSyntax
    {
        /// <summary>
        /// Add Item
        /// </summary>
        /// <param name="value"></param>
        /// <param name="item"></param>
        /// <param name="orderIndex"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        IUpdateReferenceListSyntax AddItem(long value, string item, Int64? orderIndex = null, string description = null);

        /// <summary>
        /// Update item
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        IUpdateReferenceListSyntax UpdateItem(long value);

        /// <summary>
        /// Delete item
        /// </summary>
        /// <param name="itemValue">Item value</param>
        /// <returns></returns>
        IUpdateReferenceListSyntax DeleteItem(Int64 itemValue);

        /// <summary>
        /// Delete all items
        /// </summary>
        /// <returns></returns>
        IUpdateReferenceListSyntax DeleteAllItems();
    }
}
