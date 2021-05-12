using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Notifications;
using Shesha.Domain;
using Shesha.Notifications.Dto;

namespace Shesha.Notifications
{
    /// <summary>
    /// Notification service
    /// </summary>
    public interface INotificationAppService: IAsyncCrudAppService<NotificationDto, Guid>
    {
        /// <summary>
        /// Publish new notification
        /// </summary>
        /// <param name="notificationName">Name of the notification</param>
        /// <param name="data">Notification data (is used in templates)</param>
        /// <param name="recipients">list of recipients</param>
        /// <returns></returns>
        Task PublishAsync(string notificationName, NotificationData data, List<Person> recipients);

        /// <summary>
        /// Publish email notification
        /// </summary>
        /// <param name="notificationName">Name of the notification. Default email template of the specified notification will be used</param>
        /// <param name="data">Data that is used to fill template</param>
        /// <param name="emailAddress">Recipient email address</param>
        /// <returns></returns>
        Task PublishEmailNotificationAsync<TData>(string notificationName, TData data, string emailAddress, List<NotificationAttachmentDto> attachments) where TData : NotificationData;

        /// <summary>
        /// Publish email notification using explicitly specified template
        /// </summary>
        /// <param name="templateId">Id of the template</param>
        /// <param name="data">Data that is used to fill template</param>
        /// <param name="emailAddress">Recipient email address</param>
        /// <returns></returns>
        Task PublishEmailNotificationAsync<TData>(Guid templateId, TData data, string emailAddress, List<NotificationAttachmentDto> attachments) where TData : NotificationData;

        /// <summary>
        /// Publish sms notification
        /// </summary>
        /// <param name="notificationName">Name of the notification. Default email template of the specified notification will be used</param>
        /// <param name="data">Data that is used to fill template</param>
        /// <param name="mobileNo">Recipient mobile number</param>
        /// <returns></returns>
        Task PublishSmsNotificationAsync<TData>(string notificationName, TData data, string mobileNo) where TData : NotificationData;

        /// <summary>
        /// Publish sms notification using explicitly specified template
        /// </summary>
        /// <param name="templateId">Id of the template</param>
        /// <param name="data">Data that is used to fill template</param>
        /// <param name="mobileNo">Recipient mobile number</param>
        /// <returns></returns>
        Task PublishSmsNotificationAsync<TData>(Guid templateId, TData data, string mobileNo) where TData : NotificationData;

        /// <summary>
        /// Save notification attachment
        /// </summary>
        Task<NotificationAttachmentDto> SaveAttachmentAsync(string fileName, Stream stream);
    }
}
