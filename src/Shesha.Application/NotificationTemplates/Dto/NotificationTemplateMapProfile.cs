using System;
using Shesha.AutoMapper;
using Shesha.AutoMapper.Dto;
using Shesha.Domain;

namespace Shesha.NotificationTemplates.Dto
{
    public class NotificationTemplateMapProfile : ShaProfile
    {
        public NotificationTemplateMapProfile()
        {
            CreateMap<NotificationTemplate, NotificationTemplateDto>()
                .ForMember(u => u.Notification, 
                    options => options.MapFrom(e => e.Notification != null ? new EntityWithDisplayNameDto<Guid?> { Id = e.Notification.Id, DisplayText = e.Notification.Name } : null))
                .MapReferenceListValuesToDto();

            CreateMap<NotificationTemplateDto, NotificationTemplate>()
                .ForMember(u => u.Notification,
                    options => options.MapFrom(e =>
                        e.Notification != null && e.Notification.Id != null
                            ? GetEntity<Notification, Guid>(e.Notification.Id.Value)
                            : null))
                .MapReferenceListValuesFromDto();

        }
    }
}
