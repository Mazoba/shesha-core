using System;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Shesha.Domain;
using Shesha.Otp.Dto;
using Shesha.Web.DataTable;

namespace Shesha.Otp
{
    /// <summary>
    /// One-time-pin audit service
    /// </summary>
    [AbpAuthorize()]
    public class OtpAuditItemAppService: SheshaCrudServiceBase<OtpAuditItem, OtpAuditItemDto, Guid>
    {
        public OtpAuditItemAppService(IRepository<OtpAuditItem, Guid> repository) : base(repository)
        {
        }

        /// <summary>
        /// Notification Templates index table
        /// </summary>
        /// <returns></returns>
        public static DataTableConfig IndexTable()
        {
            var table = new DataTableConfig<OtpAuditItem, Guid>("OtpAuditItems_Index");

            //table.AddProperty(e => e.Id, c => c.HiddenByDefault());
            table.AddProperty(e => e.CreationTime, c => c.Caption("Created On").SortDescending());

            table.AddProperty(e => e.SendTo);
            table.AddProperty(e => e.SendType);
            table.AddProperty(e => e.RecipientType, c => c.HiddenByDefault());
            table.AddProperty(e => e.RecipientId, c => c.HiddenByDefault());
            table.AddProperty(e => e.ExpiresOn);
            table.AddProperty(e => e.Otp);
            table.AddProperty(e => e.ActionType);
            table.AddProperty(e => e.SentOn);
            table.AddProperty(e => e.SendStatus);
            table.AddProperty(e => e.ErrorMessage);

            return table;
        }
    }
}
