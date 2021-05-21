using System;
using Shesha.AutoMapper;
using Shesha.AutoMapper.Dto;
using Shesha.Domain;
using Shesha.Notifications.Dto;

namespace Shesha.NotificationMessages.Dto
{
    public class NotificationTemplateMapProfile : ShaProfile
    {
        public NotificationTemplateMapProfile()
        {
            CreateMap<NotificationMessage, NotificationMessageDto>()
                .ForMember(u => u.Sender, 
                    options => options.MapFrom(e => e.Sender != null ? new EntityWithDisplayNameDto<Guid?> { Id = e.Sender.Id, DisplayText = e.Sender.FullName } : null))
                .ForMember(u => u.Recipient,
                    options => options.MapFrom(e => e.Recipient != null ? new EntityWithDisplayNameDto<Guid?> { Id = e.Recipient.Id, DisplayText = e.Recipient.FullName } : null))
                .ForMember(u => u.Template,
                    options => options.MapFrom(e => e.Template != null ? new EntityWithDisplayNameDto<Guid?> { Id = e.Template.Id, DisplayText = e.Template.Name } : null))
                .ForMember(u => u.Notification,
                    options => options.MapFrom(e => e.Notification != null ? new EntityWithDisplayNameDto<Guid?> { Id = e.Notification.Id, DisplayText = e.Notification.Name } : null))
                .MapReferenceListValuesToDto();

            CreateMap<NotificationMessageAttachment, NotificationAttachmentDto>()
                .ForMember(u => u.StoredFileId,
                    options => options.MapFrom(e => e.File != null ? e.File.Id : Guid.Empty));
        }
    }
}
