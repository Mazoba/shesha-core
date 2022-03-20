using FluentMigrator;
using FluentMigrator.Expressions;
using System;
using System.Data;

namespace Shesha.FluentMigrator.Notifications
{
    /// <summary>
    /// Notification fluent interface
    /// </summary>
    public class AddNotificationExpression : MigrationExpressionBase
    {
        public string Name { get; set; }
        public string Namespace { get; set; }
        public string Description { get; set; }
        public override void ExecuteWith(IMigrationProcessor processor)
        {
            var exp = new PerformDBOperationExpression() { Operation = (connection, transaction) => {
                var helper = new NotificationAdoHelper(connection, transaction);
                var refListId = helper.InsertNotification(Namespace, Name, Description);
            }
            };
            processor.Process(exp);
        }
    }
}
