using FluentMigrator.Builders;
using FluentMigrator.Infrastructure;
using System;

namespace Shesha.FluentMigrator.ReferenceLists
{
    public class UpdateReferenceListExpressionBuilder : ExpressionBuilderBase<UpdateReferenceListExpression>, IUpdateReferenceListSyntax
    {
        private readonly IMigrationContext _context;

        public UpdateReferenceListExpressionBuilder(UpdateReferenceListExpression expression, IMigrationContext context) : base(expression)
        {
            _context = context;
        }

        public IUpdateReferenceListSyntax AddItem(long value, string item, Int64? orderIndex = null, string description = null)
        {
            var listItem = new ReferenceListItemDefinition
            {
                Item = item,
                ItemValue = value,
                OrderIndex = orderIndex,
                Description = description
            };
            var addRefListItem = new AddReferenceListItemExpression
            {
                Item = listItem,
                Namespace = Expression.Namespace,
                Name = Expression.Name
            };

            _context.Expressions.Add(addRefListItem);

            return this;
        }

        public IUpdateReferenceListSyntax DeleteItem(Int64 itemValue) 
        {
            _context.Expressions.Add(new DeleteReferenceListItemExpression
            {
                Namespace = Expression.Namespace,
                Name = Expression.Name,
                ItemValue = itemValue
            });

            return this;
        }        

        public IUpdateReferenceListSyntax DeleteAllItems()
        {
            _context.Expressions.Add(new DeleteReferenceListItemExpression
            {
                Namespace = Expression.Namespace,
                Name = Expression.Name,
                DeleteAll = true
            });

            return this;
        }

        public IUpdateReferenceListSyntax UpdateItem(long value)
        {
            throw new NotImplementedException();
        }
    }
}
