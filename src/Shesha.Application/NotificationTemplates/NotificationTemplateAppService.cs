using System;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;
using Shesha.Domain;
using Shesha.NotificationTemplates.Dto;
using Shesha.Web.DataTable;
using Shesha.Web.DataTable;

namespace Shesha.NotificationTemplates
{
    public class NotificationTemplateAppService : SheshaCrudServiceBase<NotificationTemplate, NotificationTemplateDto, Guid>
    {
        public NotificationTemplateAppService(IRepository<NotificationTemplate, Guid> repository) : base(repository)
        {
        }

        /// <summary>
        /// Notification Templates index table
        /// </summary>
        /// <returns></returns>
        public static DataTableConfig IndexTable()
        {
            var table = new DataTableConfig<NotificationTemplate, Guid>("NotificationTemplates_Index");

            table.AddProperty(e => e.Name, c => c.SortAscending());
            table.AddProperty(e => e.SendType);
            table.AddProperty(e => e.BodyFormat);
            table.AddProperty(e => e.Subject);
            table.AddProperty(e => e.Body);

            table.AddProperty(e => e.CreationTime, c => c.Caption("Created On").Visible(false));
            table.AddProperty(e => e.LastModificationTime, c => c.Caption("Updated On").Visible(false));

            return table;
        }

        /// <summary>
        /// Appointed persons child table
        /// </summary>
        public static ChildDataTableConfig<Notification, NotificationTemplate, Guid> ShaRoleAppointedPersonsTable()
        {
            var table = ChildDataTableConfig<Notification, NotificationTemplate, Guid>.OneToMany("Notification_Templates", ap => ap.Notification);

            table.AddProperty(e => e.IsEnabled);
            table.AddProperty(e => e.SendType);
            table.AddProperty(e => e.Name);
            table.AddProperty(e => e.Subject);
            table.AddProperty(e => e.BodyFormat);
            table.AddProperty(e => e.Body);

            table.AddProperty(e => e.CreationTime, c => c.Caption("Created On").Visible(false));
            table.AddProperty(e => e.LastModificationTime, c => c.Caption("Updated On").Visible(false));

            return table;
        }

        [HttpPost] // note: have to use POST verb just because of restriction of the restful-react
        public override async Task DeleteAsync(EntityDto<Guid> input)
        {
            await base.DeleteAsync(input);
        }

        public override Task<NotificationTemplateDto> UpdateAsync(NotificationTemplateDto input)
        {
            return base.UpdateAsync(input);
        }
    }
}