using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp;
using Abp.Collections.Extensions;
using Abp.Configuration;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Services;
using Abp.Domain.Uow;
using Abp.Extensions;
using Abp.Notifications;
using Abp.Runtime.Session;
using Shesha.Domain;
using Shesha.Domain.Enums;
using Shesha.NotificationMessages.Dto;
using Shesha.Notifications.Dto;

namespace Shesha.Notifications
{
    public class ShaNotificationDistributer : DomainService, IShaNotificationDistributer
    {
        private readonly INotificationConfiguration _notificationConfiguration;
        private readonly INotificationDefinitionManager _notificationDefinitionManager;
        private readonly INotificationStore _notificationStore;
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly IGuidGenerator _guidGenerator;
        private readonly IIocResolver _iocResolver;
        private readonly IRepository<NotificationMessage, Guid> _messageRepository;
        private readonly IRepository<StoredFile, Guid> _storedFileRepository;
        private readonly IRepository<NotificationMessageAttachment, Guid> _attachmentRepository;
        
        public IAbpSession AbpSession { get; set; } = NullAbpSession.Instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationDistributionJob"/> class.
        /// </summary>
        public ShaNotificationDistributer(
            INotificationConfiguration notificationConfiguration,
            INotificationDefinitionManager notificationDefinitionManager,
            INotificationStore notificationStore,
            IUnitOfWorkManager unitOfWorkManager,
            IGuidGenerator guidGenerator,
            IIocResolver iocResolver, IRepository<NotificationMessage, Guid> messageRepository, IRepository<StoredFile, Guid> storedFileRepository, IRepository<NotificationMessageAttachment, Guid> attachmentRepository)
        {
            _notificationConfiguration = notificationConfiguration;
            _notificationDefinitionManager = notificationDefinitionManager;
            _notificationStore = notificationStore;
            _unitOfWorkManager = unitOfWorkManager;
            _guidGenerator = guidGenerator;
            _iocResolver = iocResolver;
            _messageRepository = messageRepository;
            _storedFileRepository = storedFileRepository;
            _attachmentRepository = attachmentRepository;
        }

        public async Task DistributeAsync(Guid notificationId)
        {
            var notificationInfo = await _notificationStore.GetNotificationOrNullAsync(notificationId);
            if (notificationInfo == null)
            {
                Logger.Warn("NotificationDistributionJob can not continue since could not found notification by id: " + notificationId);
                return;
            }

            var users = await GetUsersAsync(notificationInfo);
            if (users.Any())
            {
                var userNotifications = await SaveUserNotificationsAsync(users, notificationInfo);

                await _notificationStore.DeleteNotificationAsync(notificationInfo);

                await NotifyAsync(userNotifications.ToArray());
            }
            else
            {
                // handle Shesha messages without recipients
                // todo: combine with subscriptions
                var notificationMessages = await SaveUserNotificationMessagesAsync(notificationInfo);
                await NotifyAsync(notificationMessages);
            }
        }

        public void Distribute(Guid notificationId)
        {
            var notificationInfo = _notificationStore.GetNotificationOrNull(notificationId);
            if (notificationInfo == null)
            {
                Logger.Warn("NotificationDistributionJob can not continue since could not found notification by id: " + notificationId);
                return;
            }

            var users = GetUsers(notificationInfo);
            if (users.Any())
            {
                var userNotifications = SaveUserNotifications(users, notificationInfo);

                _notificationStore.DeleteNotification(notificationInfo);

                Notify(userNotifications.ToArray());
            }
            else
            {
                // handle Shesha messages without recipients
                // todo: combine with subscriptions
                var notificationMessages = SaveNotificationMessages(notificationInfo);
                Notify(notificationMessages);
            }
        }

        /// inheritedDoc
        public async Task ResendMessageAsync(NotificationMessageDto notificationMessage)
        {
            var notifierTypes = _notificationConfiguration.Notifiers
                .Where(t => typeof(IShaRealTimeNotifier).IsAssignableFrom(t)).ToList();
            foreach (var notifierType in notifierTypes)
            {
                try
                {
                    using (var notifier = _iocResolver.ResolveAsDisposable<IShaRealTimeNotifier>(notifierType))
                    {
                        if ((int)notifier.Object.NotificationType == notificationMessage.SendType?.ItemValue)
                            await notifier.Object.ResendMessageAsync(notificationMessage);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex.ToString(), ex);
                }
            }
        }

        [UnitOfWork]
        protected virtual async Task<UserIdentifier[]> GetUsersAsync(NotificationInfo notificationInfo)
        {
            List<UserIdentifier> userIds;

            if (!notificationInfo.UserIds.IsNullOrEmpty())
            {
                //Directly get from UserIds
                userIds = notificationInfo
                    .UserIds
                    .Split(",")
                    .Select(uidAsStr => UserIdentifier.Parse(uidAsStr))
                    .Where(uid => SettingManager.GetSettingValueForUser<bool>(NotificationSettingNames.ReceiveNotifications, uid.TenantId, uid.UserId))
                    .ToList();
            }
            else
            {
                //Get subscribed users

                var tenantIds = GetTenantIds(notificationInfo);

                List<NotificationSubscriptionInfo> subscriptions;

                if (tenantIds.IsNullOrEmpty() ||
                    (tenantIds.Length == 1 && tenantIds[0] == NotificationInfo.AllTenantIds.To<int>()))
                {
                    //Get all subscribed users of all tenants
                    subscriptions = await _notificationStore.GetSubscriptionsAsync(
                        notificationInfo.NotificationName,
                        notificationInfo.EntityTypeName,
                        notificationInfo.EntityId
                        );
                }
                else
                {
                    //Get all subscribed users of specified tenant(s)
                    subscriptions = await _notificationStore.GetSubscriptionsAsync(
                        tenantIds,
                        notificationInfo.NotificationName,
                        notificationInfo.EntityTypeName,
                        notificationInfo.EntityId
                        );
                }

                //Remove invalid subscriptions
                var invalidSubscriptions = new Dictionary<Guid, NotificationSubscriptionInfo>();

                //TODO: Group subscriptions per tenant for potential performance improvement
                foreach (var subscription in subscriptions)
                {
                    using (CurrentUnitOfWork.SetTenantId(subscription.TenantId))
                    {
                        if (!await _notificationDefinitionManager.IsAvailableAsync(notificationInfo.NotificationName, new UserIdentifier(subscription.TenantId, subscription.UserId)) ||
                            !SettingManager.GetSettingValueForUser<bool>(NotificationSettingNames.ReceiveNotifications, subscription.TenantId, subscription.UserId))
                        {
                            invalidSubscriptions[subscription.Id] = subscription;
                        }
                    }
                }

                subscriptions.RemoveAll(s => invalidSubscriptions.ContainsKey(s.Id));

                //Get user ids
                userIds = subscriptions
                    .Select(s => new UserIdentifier(s.TenantId, s.UserId))
                    .ToList();
            }

            if (!notificationInfo.ExcludedUserIds.IsNullOrEmpty())
            {
                //Exclude specified users.
                var excludedUserIds = notificationInfo
                    .ExcludedUserIds
                    .Split(",")
                    .Select(uidAsStr => UserIdentifier.Parse(uidAsStr))
                    .ToList();

                userIds.RemoveAll(uid => excludedUserIds.Any(euid => euid.Equals(uid)));
            }

            return userIds.ToArray();
        }

        [UnitOfWork]
        protected virtual UserIdentifier[] GetUsers(NotificationInfo notificationInfo)
        {
            List<UserIdentifier> userIds;

            if (!notificationInfo.UserIds.IsNullOrEmpty())
            {
                //Directly get from UserIds
                userIds = notificationInfo
                    .UserIds
                    .Split(",")
                    .Select(uidAsStr => UserIdentifier.Parse(uidAsStr))
                    .Where(uid => SettingManager.GetSettingValueForUser<bool>(NotificationSettingNames.ReceiveNotifications, uid.TenantId, uid.UserId))
                    .ToList();
            }
            else
            {
                //Get subscribed users

                var tenantIds = GetTenantIds(notificationInfo);

                List<NotificationSubscriptionInfo> subscriptions;

                if (tenantIds.IsNullOrEmpty() ||
                    (tenantIds.Length == 1 && tenantIds[0] == NotificationInfo.AllTenantIds.To<int>()))
                {
                    //Get all subscribed users of all tenants
                    subscriptions = _notificationStore.GetSubscriptions(
                        notificationInfo.NotificationName,
                        notificationInfo.EntityTypeName,
                        notificationInfo.EntityId
                        );
                }
                else
                {
                    //Get all subscribed users of specified tenant(s)
                    subscriptions = _notificationStore.GetSubscriptions(
                        tenantIds,
                        notificationInfo.NotificationName,
                        notificationInfo.EntityTypeName,
                        notificationInfo.EntityId
                        );
                }

                //Remove invalid subscriptions
                var invalidSubscriptions = new Dictionary<Guid, NotificationSubscriptionInfo>();

                //TODO: Group subscriptions per tenant for potential performance improvement
                foreach (var subscription in subscriptions)
                {
                    using (CurrentUnitOfWork.SetTenantId(subscription.TenantId))
                    {
                        if (!_notificationDefinitionManager.IsAvailable(notificationInfo.NotificationName, new UserIdentifier(subscription.TenantId, subscription.UserId)) ||
                            !SettingManager.GetSettingValueForUser<bool>(NotificationSettingNames.ReceiveNotifications, subscription.TenantId, subscription.UserId))
                        {
                            invalidSubscriptions[subscription.Id] = subscription;
                        }
                    }
                }

                subscriptions.RemoveAll(s => invalidSubscriptions.ContainsKey(s.Id));

                //Get user ids
                userIds = subscriptions
                    .Select(s => new UserIdentifier(s.TenantId, s.UserId))
                    .ToList();
            }

            if (!notificationInfo.ExcludedUserIds.IsNullOrEmpty())
            {
                //Exclude specified users.
                var excludedUserIds = notificationInfo
                    .ExcludedUserIds
                    .Split(",")
                    .Select(uidAsStr => UserIdentifier.Parse(uidAsStr))
                    .ToList();

                userIds.RemoveAll(uid => excludedUserIds.Any(euid => euid.Equals(uid)));
            }

            return userIds.ToArray();
        }

        private static int?[] GetTenantIds(NotificationInfo notificationInfo)
        {
            if (notificationInfo.TenantIds.IsNullOrEmpty())
            {
                return new int?[]{ null };
            }

            return notificationInfo
                .TenantIds
                .Split(",")
                .Select(tenantIdAsStr => tenantIdAsStr == "null" ? (int?)null : (int?)tenantIdAsStr.To<int>())
                .ToArray();
        }

        [UnitOfWork]
        protected virtual async Task<List<UserNotification>> SaveUserNotificationsAsync(UserIdentifier[] users, NotificationInfo notificationInfo)
        {
            var userNotifications = new List<UserNotification>();

            var tenantGroups = users.GroupBy(user => user.TenantId);
            foreach (var tenantGroup in tenantGroups)
            {
                using (_unitOfWorkManager.Current.SetTenantId(tenantGroup.Key))
                {
                    var tenantNotificationInfo = new TenantNotificationInfo(_guidGenerator.Create(), tenantGroup.Key, notificationInfo);
                    await _notificationStore.InsertTenantNotificationAsync(tenantNotificationInfo);
                    await _unitOfWorkManager.Current.SaveChangesAsync(); //To get tenantNotification.Id.

                    var tenantNotification = tenantNotificationInfo.ToTenantNotification();

                    foreach (var user in tenantGroup)
                    {
                        var userNotification = new UserNotificationInfo(_guidGenerator.Create())
                        {
                            TenantId = tenantGroup.Key,
                            UserId = user.UserId,
                            TenantNotificationId = tenantNotificationInfo.Id
                        };

                        await _notificationStore.InsertUserNotificationAsync(userNotification);
                        userNotifications.Add(userNotification.ToUserNotification(tenantNotification));
                    }

                    await CurrentUnitOfWork.SaveChangesAsync(); //To get Ids of the notifications
                }
            }

            return userNotifications;
        }

        [UnitOfWork]
        protected virtual async Task<List<NotificationMessageDto>> SaveUserNotificationMessagesAsync(NotificationInfo notificationInfo)
        {
            var notificationMessages = new List<NotificationMessageDto>();

            var tenantIds = GetTenantIds(notificationInfo);
            foreach (var tenantId in tenantIds)
            {
                using (_unitOfWorkManager.Current.SetTenantId(tenantId))
                {
                    var tenantNotificationInfo = new TenantNotificationInfo(_guidGenerator.Create(), tenantId, notificationInfo);
                    await _notificationStore.InsertTenantNotificationAsync(tenantNotificationInfo);
                    await _unitOfWorkManager.Current.SaveChangesAsync(); //To get tenantNotification.Id.

                    var tenantNotification = tenantNotificationInfo.ToTenantNotification();

                    if (tenantNotification.Data is ShaNotificationData shaData)
                    {
                        await _notificationStore.InsertTenantNotificationAsync(tenantNotificationInfo);
                        await _unitOfWorkManager.Current.SaveChangesAsync(); //To get tenantNotification.Id.

                        var notificationMessage = new NotificationMessage
                        {
                            TenantNotification = tenantNotificationInfo,
                            SendType = shaData.SendType,
                            RecipientText = shaData.RecipientText,
                            Status = RefListNotificationStatus.Preparing
                        };
                        await _messageRepository.InsertAsync(notificationMessage);

                        var notificationMessageDto = ObjectMapper.Map<NotificationMessageDto>(notificationMessage);

                        // save attachments if specified
                        if (shaData.Attachments != null)
                        {
                            foreach (var attachmentDto in shaData.Attachments)
                            {
                                var file = await _storedFileRepository.GetAsync(attachmentDto.StoredFileId);
                                var attachment = new NotificationMessageAttachment
                                {
                                    Message = notificationMessage,
                                    File = file,
                                    FileName = attachmentDto.FileName
                                };
                                await _attachmentRepository.InsertAsync(attachment);

                                notificationMessageDto.Attachments.Add(ObjectMapper.Map<NotificationAttachmentDto>(attachment));
                            }
                        }

                        notificationMessages.Add(notificationMessageDto);

                        await CurrentUnitOfWork.SaveChangesAsync(); //To get Ids of the notifications
                    }
                }
            }

            return notificationMessages;
        }

        [UnitOfWork]
        protected virtual List<NotificationMessageDto> SaveNotificationMessages(NotificationInfo notificationInfo)
        {
            var notificationMessages = new List<NotificationMessageDto>();

            var tenantIds = GetTenantIds(notificationInfo) ?? new int?[]{ AbpSession.TenantId };
            foreach (var tenantId in tenantIds)
            {
                using (_unitOfWorkManager.Current.SetTenantId(tenantId))
                {
                    var tenantNotificationInfo = new TenantNotificationInfo(_guidGenerator.Create(), tenantId, notificationInfo);
                    var tenantNotification = tenantNotificationInfo.ToTenantNotification();

                    if (tenantNotification.Data is ShaNotificationData shaData)
                    {
                        _notificationStore.InsertTenantNotification(tenantNotificationInfo);
                        _unitOfWorkManager.Current.SaveChanges(); //To get tenantNotification.Id.

                        var notificationMessage = new NotificationMessage
                        {
                            TenantNotification = tenantNotificationInfo,
                            SendType = shaData.SendType,
                            RecipientText = shaData.RecipientText,
                            Status = RefListNotificationStatus.Preparing
                        };
                        _messageRepository.Insert(notificationMessage);
                        notificationMessages.Add(ObjectMapper.Map<NotificationMessageDto>(notificationMessage));
                        
                        CurrentUnitOfWork.SaveChanges(); //To get Ids of the notifications
                    }
                }
            }

            return notificationMessages;
        }

        [UnitOfWork]
        protected virtual List<UserNotification> SaveUserNotifications(UserIdentifier[] users, NotificationInfo notificationInfo)
        {
            var userNotifications = new List<UserNotification>();

            var tenantGroups = users.GroupBy(user => user.TenantId);
            foreach (var tenantGroup in tenantGroups)
            {
                using (_unitOfWorkManager.Current.SetTenantId(tenantGroup.Key))
                {
                    var tenantNotificationInfo = new TenantNotificationInfo(_guidGenerator.Create(), tenantGroup.Key, notificationInfo);
                    _notificationStore.InsertTenantNotification(tenantNotificationInfo);
                    _unitOfWorkManager.Current.SaveChanges(); //To get tenantNotification.Id.

                    var tenantNotification = tenantNotificationInfo.ToTenantNotification();

                    foreach (var user in tenantGroup)
                    {
                        var userNotification = new UserNotificationInfo(_guidGenerator.Create())
                        {
                            TenantId = tenantGroup.Key,
                            UserId = user.UserId,
                            TenantNotificationId = tenantNotificationInfo.Id
                        };

                        _notificationStore.InsertUserNotification(userNotification);
                        userNotifications.Add(userNotification.ToUserNotification(tenantNotification));
                    }

                    CurrentUnitOfWork.SaveChanges(); //To get Ids of the notifications
                }
            }

            return userNotifications;
        }

        #region Protected methods

        protected virtual async Task NotifyAsync(UserNotification[] userNotifications)
        {
            foreach (var notifierType in _notificationConfiguration.Notifiers)
            {
                try
                {
                    using (var notifier = _iocResolver.ResolveAsDisposable<IRealTimeNotifier>(notifierType))
                    {
                        await notifier.Object.SendNotificationsAsync(userNotifications);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex.ToString(), ex);
                }
            }
        }

        protected virtual void Notify(UserNotification[] userNotifications)
        {
            foreach (var notifierType in _notificationConfiguration.Notifiers)
            {
                try
                {
                    using (var notifier = _iocResolver.ResolveAsDisposable<IRealTimeNotifier>(notifierType))
                    {
                        notifier.Object.SendNotifications(userNotifications);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex.ToString(), ex);
                }
            }
        }

        protected virtual async Task NotifyAsync(List<NotificationMessageDto> notificationMessages)
        {
            var shaNotifierTypes = _notificationConfiguration.Notifiers
                .Where(t => typeof(IShaRealTimeNotifier).IsAssignableFrom(t)).ToList();

            foreach (var notifierType in shaNotifierTypes)
            {
                try
                {
                    using (var notifier = _iocResolver.ResolveAsDisposable<IShaRealTimeNotifier>(notifierType))
                    {
                        await notifier.Object.SendNotificationsAsync(notificationMessages);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex.ToString(), ex);
                }
            }
        }

        protected virtual void Notify(List<NotificationMessageDto> notificationMessages)
        {
            var shaNotifierTypes = _notificationConfiguration.Notifiers
                .Where(t => typeof(IShaRealTimeNotifier).IsAssignableFrom(t)).ToList();

            foreach (var notifierType in shaNotifierTypes)
            {
                try
                {
                    using (var notifier = _iocResolver.ResolveAsDisposable<IShaRealTimeNotifier>(notifierType))
                    {
                        notifier.Object.SendNotifications(notificationMessages);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex.ToString(), ex);
                }
            }
        }

        #endregion        
    }
}