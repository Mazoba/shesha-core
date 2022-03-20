using FluentMigrator.Builders;
using FluentMigrator.Infrastructure;
using System;

namespace Shesha.FluentMigrator.Notifications
{
    public class UpdateNotificationExpressionBuilder : ExpressionBuilderBase<UpdateNotificationExpression>, IUpdateNotificationSyntax
    {
        private readonly IMigrationContext _context;

        public UpdateNotificationExpressionBuilder(UpdateNotificationExpression expression, IMigrationContext context) : base(expression)
        {
            _context = context;
        }

        public IUpdateNotificationSyntax DeleteTemplates()
        {
            _context.Expressions.Add(new DeleteNotificationTemplateExpression
            {
                Namespace = Expression.Namespace,
                Name = Expression.Name,
                DeleteAll = true
            });

            return this;
        }

        public IUpdateNotificationSyntax SetDescription(string description) 
        {
            Expression.Description.Set(description);
            
            return this;
        }

        public IUpdateNotificationSyntax AddEmailTemplate(Guid id, string name, string subject, string body)
        {
            var template = new NotificationTemplateDefinition();
            template.Id.Set(id);
            template.Name.Set(name);
            template.Subject.Set(subject);
            template.Body.Set(body);

            template.IsEnabled.Set(true);
            template.SendType.Set(Domain.Enums.RefListNotificationType.Email);
            template.BodyFormat.Set(Domain.Enums.RefListNotificationTemplateType.Html);

            _context.Expressions.Add(new AddNotificationTemplateExpression
            {
                Template = template,
                Namespace = Expression.Namespace,
                Name = Expression.Name
            });

            return this;
        }

        public IUpdateNotificationSyntax AddPushTemplate(Guid id, string name, string subject, string body)
        {
            var template = new NotificationTemplateDefinition();
            template.Id.Set(id);
            template.Name.Set(name);
            template.Subject.Set(subject);
            template.Body.Set(body);

            template.IsEnabled.Set(true);
            template.SendType.Set(Domain.Enums.RefListNotificationType.Push);
            template.BodyFormat.Set(Domain.Enums.RefListNotificationTemplateType.PlainText);

            _context.Expressions.Add(new AddNotificationTemplateExpression
            {
                Template = template,
                Namespace = Expression.Namespace,
                Name = Expression.Name
            });

            return this;
        }

        public IUpdateNotificationSyntax AddSmsTemplate(Guid id, string name, string body)
        {
            var template = new NotificationTemplateDefinition();
            template.Id.Set(id);
            template.Name.Set(name);
            template.Body.Set(body);

            template.IsEnabled.Set(true);
            template.SendType.Set(Domain.Enums.RefListNotificationType.SMS);
            template.BodyFormat.Set(Domain.Enums.RefListNotificationTemplateType.PlainText);

            _context.Expressions.Add(new AddNotificationTemplateExpression
            {
                Template = template,
                Namespace = Expression.Namespace,
                Name = Expression.Name
            });

            return this;
        }
    }
}
