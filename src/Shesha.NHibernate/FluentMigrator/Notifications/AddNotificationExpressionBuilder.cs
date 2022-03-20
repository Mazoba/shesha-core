using FluentMigrator.Builders;
using FluentMigrator.Infrastructure;
using System;

namespace Shesha.FluentMigrator.Notifications
{
    public class AddNotificationExpressionBuilder : ExpressionBuilderBase<AddNotificationExpression>, IAddNotificationSyntax
    {
        private readonly IMigrationContext _context;

        public AddNotificationExpressionBuilder(AddNotificationExpression expression, IMigrationContext context) : base(expression)
        {
            _context = context;
        }

        public IAddNotificationSyntax AddEmailTemplate(Guid id, string name, string subject, string body)
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

        public IAddNotificationSyntax AddPushTemplate(Guid id, string name, string subject, string body)
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

        public IAddNotificationSyntax AddSmsTemplate(Guid id, string name, string body)
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

        public IAddNotificationSyntax SetDescription(string description)
        {
            Expression.Description = description;
            return this;
        }
    }
}
