using FluentMigrator;
using FluentMigrator.Infrastructure;
using Shesha.FluentMigrator.ReferenceLists;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shesha.FluentMigrator
{
    /// <summary>
    /// ReferenceList fluent interface
    /// </summary>
    public class SheshaExpressionRoot : IFluentSyntax
    {
        private readonly IMigrationContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="SheshaExpressionRoot"/> class.
        /// </summary>
        /// <param name="context">The migration context</param>
        public SheshaExpressionRoot(IMigrationContext context)
        {
            _context = context;
        }

        public IAddReferenceListSyntax ReferenceListCreate(string @namespace, string name) 
        {
            var expression = new AddReferenceListExpression { Namespace = @namespace, Name = name };
            
            _context.Expressions.Add(expression);

            return new AddReferenceListExpressionBuilder(expression, _context);
        }
        
        public IDeleteReferenceListSyntax ReferenceListDelete(string @namespace, string name)
        {
            var expression = new DeleteReferenceListExpression { Namespace = @namespace, Name = name };

            _context.Expressions.Add(expression);

            return new DeleteReferenceListExpressionBuilder(expression, _context);
        }

        public IUpdateReferenceListSyntax ReferenceListUpdate(string @namespace, string name)
        {
            var expression = new UpdateReferenceListExpression { Namespace = @namespace, Name = name };

            _context.Expressions.Add(expression);

            return new UpdateReferenceListExpressionBuilder(expression, _context);
        }
    }
}
