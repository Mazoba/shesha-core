﻿using FluentMigrator;
using FluentMigrator.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shesha.FluentMigrator.ReferenceLists
{
    /// <summary>
    /// ReferenceListItem delete expression
    /// </summary>
    public class DeleteReferenceListItemExpression : MigrationExpressionBase
    {
        public string Name { get; set; }
        public string Namespace { get; set; }
        public bool DeleteAll { get; set; }
        public Int64? ItemValue { get; set; }

        public override void ExecuteWith(IMigrationProcessor processor)
        {
            var exp = new PerformDBOperationExpression()
            {
                Operation = (connection, transaction) =>
                {
                    var helper = new ReferenceListDbHelper(connection, transaction);

                    var refListId = helper.GetReferenceListId(Namespace, Name);
                    if (refListId == null)
                        throw new Exception($"Reference list '{Namespace}.{Name}' not found");

                    if (DeleteAll)
                    {
                        // delete all if filter is not specified
                        helper.DeleteReferenceListItems(Namespace, Name);
                    } else 
                    if (ItemValue.HasValue)
                    {
                        helper.DeleteReferenceListItem(Namespace, Name, ItemValue.Value);
                    }
                }
            };
            processor.Process(exp);
        }

    }
}
