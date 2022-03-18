using FluentMigrator;
using FluentMigrator.Expressions;
using System.Data;

namespace Shesha.FluentMigrator.ReferenceLists
{
    /// <summary>
    /// ReferenceList fluent interface
    /// </summary>
    public class AddReferenceListExpression : MigrationExpressionBase
    {
        public string Name { get; set; }
        public string Namespace { get; set; }
        public string Description { get; set; }

        public override void ExecuteWith(IMigrationProcessor processor)
        {
            var exp = new PerformDBOperationExpression() { Operation = (connection, transaction) => DbOperation(connection, transaction) };
            processor.Process(exp);
        }

        private void DbOperation(IDbConnection connection, IDbTransaction transaction) 
        {
            var helper = new ReferenceListAdoHelper(connection, transaction);
            var refListId = helper.InsertReferenceList(Namespace, Name, Description);
        }
    }
}
