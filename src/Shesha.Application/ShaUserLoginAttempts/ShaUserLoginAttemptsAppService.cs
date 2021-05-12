using System;
using Abp.Application.Services;
using Abp.Domain.Repositories;
using Shesha.Domain;
using Shesha.ShaUserLoginAttempts.Dto;
using Shesha.Web.DataTable;

namespace Shesha.ShaUserLoginAttempts
{
    public class ShaUserLoginAttemptsAppService : AsyncCrudAppService<ShaUserLoginAttempt, ShaUserLoginAttemptDto, Guid>, IShaUserLoginAttemptsAppService
    {
        public ShaUserLoginAttemptsAppService(IRepository<ShaUserLoginAttempt, Guid> repository) : base(repository)
        {
        }

        /// <summary>
        /// Index table configuration 
        /// </summary>
        public static DataTableConfig IndexTable()
        {
            var table = new DataTableConfig<ShaUserLoginAttempt, Guid>("LogonAudit_Index");

            table.AddProperty(e => e.CreationTime, c => c.Caption("Date").SortDescending());
            table.AddProperty(e => e.Result);
            table.AddProperty(e => e.UserNameOrEmailAddress);
            table.AddProperty(e => e.BrowserInfo);
            table.AddProperty(e => e.ClientIpAddress);
            table.AddProperty(e => e.DeviceName);
            table.AddProperty(e => e.IMEI);
            table.AddProperty(e => e.ClientName);
            table.AddProperty(e => e.LoginAttemptNumber);
            
            return table;
        }
    }
}

