using System;
using Shesha.AutoMapper;
using Shesha.AutoMapper.Dto;
using Shesha.Scheduler.Domain;

namespace Shesha.Scheduler.Services.ScheduledJobs.Dto
{
    public class SchedulerMapProfile: ShaProfile
    {
        public SchedulerMapProfile()
        {
            CreateMap<ScheduledJob, ScheduledJobDto>()
                .MapReferenceListValuesToDto();

            CreateMap<ScheduledJobDto, ScheduledJob>()
                .MapReferenceListValuesFromDto();


            CreateMap<ScheduledJobTrigger, ScheduledJobTriggerDto>()
                .ForMember(u => u.Job,
                    options => options.MapFrom(e => e.Job != null ? new EntityWithDisplayNameDto<Guid?> { Id = e.Job.Id, DisplayText = e.Job.JobName } : null))
                .MapReferenceListValuesToDto();

            CreateMap<ScheduledJobTriggerDto, ScheduledJobTrigger>()
                .ForMember(u => u.Job,
                    options => options.MapFrom(e =>
                        e.Job != null && e.Job.Id != null
                            ? GetEntity<ScheduledJob, Guid>(e.Job.Id.Value)
                            : null))
                .MapReferenceListValuesFromDto();

            #region executions

            CreateMap<ScheduledJobExecution, ScheduledJobExecutionDto>()
                .ForMember(u => u.StartedBy,
                    options => options.MapFrom(e => e.StartedBy != null ? new EntityWithDisplayNameDto<Int64?> { Id = e.StartedBy.Id, DisplayText = e.StartedBy.UserName } : null))
                .ForMember(u => u.Job,
                    options => options.MapFrom(e => e.Job != null ? new EntityWithDisplayNameDto<Guid?> { Id = e.Job.Id, DisplayText = e.Job.JobName } : null))
                .ForMember(u => u.Trigger,
                    options => options.MapFrom(e => e.Trigger != null ? new EntityWithDisplayNameDto<Guid?> { Id = e.Trigger.Id, DisplayText = e.Trigger.CronString } : null))
                .MapReferenceListValuesToDto();

            #endregion
        }
    }
}
