using System;
using Abp.Application.Services;
using Abp.Dependency;
using Shesha.Domain;
using Shesha.Scheduler.Domain;
using Shesha.Web.DataTable;

namespace Shesha.Tests.Datatables
{
    public class DataTableTestAppService: ApplicationService, ITransientDependency
    {
        /// <summary>
        /// Index table configuration 
        /// </summary>
        public static DataTableConfig TestTable()
        {
            var table = new DataTableConfig<Person, Guid>("InternalPersonsTest_Index");

            table.AddProperty(e => e.User.UserName, c => c.Caption("Username").SortAscending());
            table.AddProperty(e => e.FirstName);
            table.AddProperty(e => e.LastName);
            table.AddProperty(e => e.EmailAddress1);
            table.AddProperty(e => e.MobileNumber1);
            table.AddProperty(e => e.TypeOfAccount);
            table.AddProperty(e => e.AreaLevel1, c => c.Caption("Area1"));
            table.AddProperty(e => e.User.LastLoginDate, c => c.Caption("Last log in"));
            table.AddProperty(e => e.IsLocked);

            table.AddProperty(e => e.CreationTime, c => c.Caption("Created On")/*.Visible(false)*/);
            table.AddProperty(e => e.LastModificationTime, c => c.Caption("Updated On").Visible(false));
            table.AddProperty(e => e.IsContractor);

            table.OnRequestToFilterStatic = (criteria, input) =>
            {
                criteria.FilterClauses.Add($"{nameof(Person.User)} != null");
            };

            return table;
        }

        public static DataTableConfig<ScheduledJobExecution, Guid> JobExecutionsTable()
        {
            var table = new DataTableConfig<ScheduledJobExecution, Guid>("ScheduledJob_Executions_test");

            table.UseDtos = true;
            table.DetailsUrl = url => "/api/services/Scheduler/ScheduledJobExecution/Get";
            table.CreateUrl = url => "/api/services/Scheduler/ScheduledJobExecution/Create";
            table.UpdateUrl = url => "/api/services/Scheduler/ScheduledJobExecution/Update";
            table.DeleteUrl = url => "/api/services/Scheduler/ScheduledJobExecution/Delete";

            table.AddProperty(e => e.StartedOn, m => m.SortDescending());

            //table.AddHtmlColumn("triggerLink",
            //    (e, html, url) => e.Trigger != null
            //        ? html.SPAActionLink($"{e.Trigger.Description} ({e.Trigger.CronString})", "Details", "ScheduledJobTrigger", new { id = e.Trigger.Id }).ToString()
            //        : "-", c => c.Caption("Trigger"));

            table.AddProperty(e => e.FinishedOn);
            table.AddProperty(e => e.Status);
            table.AddProperty(e => e.StartedBy);
            table.AddProperty(e => e.ErrorMessage);
            //table.AddHtmlColumn("LogFile",
            //    (e, html, url) => !string.IsNullOrWhiteSpace(e.LogFilePath)
            //        ? html.Shesha().AjaxDownload(url.Action("Download", "VirtualFile", new { path = e.LogFilePath }), Path.GetFileName(e.LogFilePath)).ToHtmlString()
            //        : null,
            //    t => t.Caption("Log File").WidthPixels(150));

            return table;
        }

        public const string PersonsTableId = "Persons_test";

        /// <summary>
        /// Index table configuration 
        /// </summary>
        public static DataTableConfig IndexTable()
        {
            var table = new DataTableConfig<Person, Guid>(PersonsTableId);

            table.AddProperty(e => e.User.UserName, c => c.Caption("Username").SortAscending());
            table.AddProperty(e => e.FirstName);
            table.AddProperty(e => e.LastName);
            table.AddProperty(e => e.EmailAddress1);
            table.AddProperty(e => e.MobileNumber1);
            table.AddProperty(e => e.TypeOfAccount);
            table.AddProperty(e => e.PrimaryOrganisation, c => c.Caption("Service Provider"));
            table.AddProperty(e => e.User.LastLoginDate, c => c.Caption("Last log in"));
            table.AddProperty(e => e.IsLocked);

            table.AddProperty(e => e.CreationTime, c => c.Caption("Created On")/*.Visible(false)*/);
            table.AddProperty(e => e.LastModificationTime, c => c.Caption("Updated On").Visible(false));
            table.AddProperty(e => e.IsContractor);

            table.OnRequestToFilterStatic = (criteria, input) =>
            {
                criteria.FilterClauses.Add($"{nameof(Person.User)} != null");
            };

            return table;
        }
    }
}
