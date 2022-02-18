using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Dependency;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using FluentMigrator.Builders.Alter.Table;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Shesha.Scheduler.Domain;
using Shesha.Scheduler.Services.ScheduledJobs.Dto;
using Shesha.Scheduler.SignalR;
using Shesha.Services;
using Shesha.Web.DataTable;

namespace Shesha.Scheduler.Services.ScheduledJobs
{
    public class ScheduledJobExecutionAppService : AsyncCrudAppService<ScheduledJobExecution, ScheduledJobExecutionDto, Guid>, ITransientDependency
    {
        private readonly IMimeMappingService _mimeMappingService;

        public ScheduledJobExecutionAppService(IRepository<ScheduledJobExecution, Guid> repository, IMimeMappingService mimeMappingService) : base(repository)
        {
            _mimeMappingService = mimeMappingService;
        }

        public static ChildDataTableConfig<ScheduledJob, ScheduledJobExecution, Guid> JobExecutionsTable()
        {
            var table = ChildDataTableConfig<ScheduledJob, ScheduledJobExecution, Guid>.OneToMany("ScheduledJob_Executions", ex => ex.Job);

            table.UseDtos = true;
            table.DetailsUrl = url => "/api/services/Scheduler/ScheduledJobExecution/Get";
            table.CreateUrl = url => "/api/services/Scheduler/ScheduledJobExecution/Create";
            table.UpdateUrl = url => "/api/services/Scheduler/ScheduledJobExecution/Update";
            table.DeleteUrl = url => "/api/services/Scheduler/ScheduledJobExecution/Delete";

            table.AddProperty(e => e.StartedOn, m => m.SortDescending());
            table.AddProperty(e => e.FinishedOn);
            table.AddProperty(e => e.Status);
            table.AddProperty(e => e.StartedBy);
            table.AddProperty(e => e.ErrorMessage);

            return table;
        }

        public static DataTableConfig<ScheduledJobExecution, Guid> JobsExecutionLog()
        {
            var table = ChildDataTableConfig<ScheduledJob, ScheduledJobExecution, Guid>.OneToMany("ScheduledJobs_ExecutionLog", ex => ex.Job);

            table.AddProperty(e => e.StartedOn, m => m.SortDescending());
            table.AddProperty(e => e.Job.JobNamespace, c => c.Caption("Job Namespace"));
            table.AddProperty(e => e.Job.JobName, c => c.Caption("Job Name"));
            table.AddProperty(e => e.FinishedOn);
            table.AddProperty(e => e.Status);
            table.AddProperty(e => e.StartedBy);
            table.AddProperty(e => e.ErrorMessage);

            return table;
        }

        /// <summary>
        /// Get event log items for the specified job execution
        /// </summary>
        /// <returns></returns>
        public async Task<List<EventLogItem>> GetEventLogItems(Guid id)
        {
            if (id == Guid.Empty)
                return new List<EventLogItem>();

            var execution = await Repository.GetAsync(id);

            if (!File.Exists(execution.LogFilePath))
                throw new Exception("Log file not found");

            var logFileContent = await File.ReadAllTextAsync(execution.LogFilePath);
            var logItems = JsonConvert.DeserializeObject<List<EventLogItem>>("[" + logFileContent + "]");

            return logItems;
        }

        /// <summary>
        /// Download log file of the job execution
        /// </summary>
        /// <param name="id">Id of the scheduled job execution</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<FileStreamResult> DownloadLogFileAsync(Guid id)
        {
            var jobExecution = await Repository.GetAsync(id);
            
            if (string.IsNullOrWhiteSpace(jobExecution.LogFilePath))
                throw new EntityNotFoundException("Path to the log file for the specified job execution is not specified");

            if (!File.Exists(jobExecution.LogFilePath))
                throw new EntityNotFoundException("Log file is missing on disk");

            var stream = new FileStream(jobExecution.LogFilePath, FileMode.Open);
            var fileName = Path.GetFileName(jobExecution.LogFilePath);
            var contentType = _mimeMappingService.Map(fileName);
            return new FileStreamResult(stream, contentType)
            {
                FileDownloadName = fileName
            };
        }
    }
}
