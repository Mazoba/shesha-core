using FluentMigrator;
using FluentMigrator.Expressions;
using System;
using System.Collections.Generic;
using System.Data;

namespace Shesha.FluentMigrator.ReferenceLists
{
    /// <summary>
    /// ReferenceList update expression
    /// </summary>
    public class UpdateReferenceListExpression : MigrationExpressionBase
    {
        public string Name { get; set; }
        public string Namespace { get; set; }

        public override void ExecuteWith(IMigrationProcessor processor)
        {
            var exp = new PerformDBOperationExpression() { Operation = (connection, transaction) => 
                {
                    var helper = new ReferenceListAdoHelper(connection, transaction);
                    var id = helper.GetReferenceListId(Namespace, Name);
                    if (id == null)
                        return;
                } 
            };
            processor.Process(exp);
        }
    }
}
