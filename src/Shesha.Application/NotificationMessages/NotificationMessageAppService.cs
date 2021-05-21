using System;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Notifications;
using Shesha.Domain;
using Shesha.NotificationMessages.Dto;
using Shesha.Notifications;
using Shesha.Services;
using Shesha.Web.DataTable;

namespace Shesha.NotificationMessages
{
    /// <summary>
    /// Notifications audit service
    /// </summary>
    public class NotificationMessageAppService : SheshaCrudServiceBase<NotificationMessage, NotificationMessageDto, Guid>
    {
        private readonly IShaNotificationDistributer _distributer;

        public NotificationMessageAppService(IRepository<NotificationMessage, Guid> repository, IShaNotificationDistributer distributer) : base(repository)
        {
            _distributer = distributer;
        }

        /// <summary>
        /// Notification Templates index table
        /// </summary>
        /// <returns></returns>
        public static DataTableConfig IndexTable()
        {
            var table = new DataTableConfig<NotificationMessage, Guid>("NotificationMessages_Index");

            table.AddProperty(e => e.CreationTime, c => c.Caption("Created On").SortDescending());
            table.AddProperty(e => e.Notification.Name, c => c.Caption("Notification"));

            table.AddProperty(e => e.Recipient);
            table.AddProperty(e => e.SendType);
            table.AddProperty(e => e.RecipientText);
            
            table.AddProperty(e => e.Subject);
            table.AddProperty(e => e.Body);

            return table;
        }

        /// <summary>
        /// Resend notification message with <see cref=""/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<bool> Resend(Guid id)
        {
            var notificationMessage = await Repository.GetAsync(id);

            var dto = ObjectMapper.Map<NotificationMessageDto>(notificationMessage);
            await _distributer.ResendMessageAsync(dto);

            return true;
    }
    }
}