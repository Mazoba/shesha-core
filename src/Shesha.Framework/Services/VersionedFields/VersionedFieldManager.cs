using Abp.Dependency;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using NHibernate.Linq;
using Shesha.Configuration.Runtime;
using Shesha.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shesha.Services.VersionedFields
{
    /// <summary>
    /// Versioned field manager
    /// </summary>
    public class VersionedFieldManager: IVersionedFieldManager, ITransientDependency
    {
        private readonly IRepository<VersionedField, Guid> _fieldRepository;
        private readonly IRepository<VersionedFieldVersion, Guid> _fieldVersionRepository;
        private readonly IEntityConfigurationStore _entityConfigurationStore;
        private readonly ICurrentUnitOfWorkProvider _currentUowProvider;

        public VersionedFieldManager(IRepository<VersionedField, Guid> fieldRepository, IRepository<VersionedFieldVersion, Guid> fieldVersionRepository, IEntityConfigurationStore entityConfigurationStore, ICurrentUnitOfWorkProvider currentUowProvider)
        {
            _fieldRepository = fieldRepository;
            _fieldVersionRepository = fieldVersionRepository;
            _entityConfigurationStore = entityConfigurationStore;
            _currentUowProvider = currentUowProvider;
        }

        public async Task<VersionedField> GetVersionedFieldAsync<TEntity, TId>(TEntity owner, string fieldName) where TEntity : IEntity<TId>
        {
            var config = _entityConfigurationStore.Get(typeof(TEntity));

            return await _fieldRepository.GetAll()
                .FirstOrDefaultAsync(f => f.OwnerId == owner.Id.ToString() && f.OwnerType == config.TypeShortAlias && f.Name == fieldName);
        }

        /// <summary>
        /// Creates versioned field is missing
        /// </summary>
        public async Task<VersionedField> GetOrCreateFieldAsync<TEntity, TId>(TEntity owner, string fieldName, Action<VersionedField> initAction = null) where TEntity : IEntity<TId>
        {
            var field = await GetVersionedFieldAsync<TEntity, TId>(owner, fieldName);
            if (field != null)
                return field;

            // todo: add unique constraint to the VersionedField: OwnerId, OwnerType, Name
            // convert lock to distributed lock on the multiple instances environment

            field = await CreateFieldAsync<TEntity, TId>(owner, fieldName, initAction);

            return field;
            /*
            lock (_locker.GetLock(owner.FullyQualifiedEntityId() + fieldName))
            {
                field = owner.GetVersionedField(fieldName);
                if (field != null)
                    return field;

                // Important: create field in a separate thread to handle unique constraint
                var task = Task.Factory.StartNew(() =>
                {
                    var sessionFactory = DependencyResolver.Current.GetService<ISessionFactory>();
                    using (var session = sessionFactory.OpenSession())
                    {
                        var service = new ServiceWithTypedId<VersionedField, Guid>();
                        service.SetSession(session);
                        var newField = new VersionedField()
                        {
                            Name = fieldName,
                            TrackVersions = trackVersions,
                            FieldType = fieldType
                        };
                        newField.SetOwner(owner);
                        session.SaveOrUpdate(newField);
                        session.Flush();
                        session.Close();
                    }
                });

                Task.WaitAll(task);

                // retrieve field using the current session
                field = owner.GetVersionedField(fieldName);
                if (field == null)
                    throw new Exception($"Failed to create versioned field, owner: {owner.FullyQualifiedEntityId()}, field: {fieldName}");
                return field;
            }*/
        }

        public async Task<VersionedField> CreateFieldAsync<TEntity, TId>(TEntity owner, string fieldName, Action<VersionedField> initAction = null) where TEntity : IEntity<TId>
        {
            var field = new VersionedField {
                Name = fieldName,
            };
            initAction?.Invoke(field);
            field.SetOwner(owner);
            await _fieldRepository.InsertAsync(field);
            await _currentUowProvider.Current.SaveChangesAsync();

            return field;
        }

        public async Task<VersionedFieldVersion> GetLastVersionAsync(VersionedField field)
        {
            var version = await _fieldVersionRepository.GetAll().Where(v => v.Field == field).OrderByDescending(f => f.CreationTime).FirstOrDefaultAsync();
            return version;
        }

        public async Task<string> GetVersionedFieldValueAsync<TEntity, TId>(TEntity owner, string fieldName) where TEntity : IEntity<TId>
        {
            //var field = await GetOrCreateFieldAsync<TEntity, TId>(owner, fieldName);
            var field = await GetVersionedFieldAsync<TEntity, TId>(owner, fieldName);
            var version = field != null
                ? await GetLastVersionAsync(field)
                : null;

            return version?.Content;
        }

        public async Task SetVersionedFieldValueAsync<TEntity, TId>(TEntity owner, string fieldName, string value, bool createNewVersion) where TEntity : IEntity<TId>
        {
            var field = await GetOrCreateFieldAsync<TEntity, TId>(owner, fieldName);
            
            var version = await GetLastVersionAsync(field);
            
            // check content of the last version and skip if not changed
            if (version != null && version.Content == value)
                return;

            if (createNewVersion || version == null)
            {
                var newVersion = new VersionedFieldVersion()
                {
                    Field = field,
                    Content = value,
                };
                await _fieldVersionRepository.InsertAsync(newVersion);
            }
            else
            {
                version.Content = value;
                await _fieldVersionRepository.UpdateAsync(version);
            }
            await _currentUowProvider.Current.SaveChangesAsync();

            /*
            lock (_locker.GetLock(owner.FullyQualifiedEntityId()))
            {
                var field = owner.GetOrCreateField(fieldName, trackVersions, fieldType);

                var version = field.LastVersion;

                modified = !version.ContentEqualsTo(value);
                if (!modified)
                    return;

                if (createNewVersion || version == null)
                {
                    var newVersion = new VersionedFieldVersion()
                    {
                        Field = field,
                        Status = status,
                        Content = value,
                        FieldType = fieldType
                    };
                    if (createdOn.HasValue)
                    {
                        newVersion.ChangeCreationAuditInfo(createdOn, userName);
                    }
                    field.Versions.Add(newVersion);
                    VersionService.SaveOrUpdateAssert(newVersion);
                }
                else
                {
                    version.Content = value;
                    version.FieldType = fieldType;
                    if (createdOn.HasValue)
                    {
                        version.ChangeCreationAuditInfo(createdOn, userName);
                    }
                    VersionService.SaveOrUpdateAssert(version);
                }

                DeleteUnusedImages(owner, fieldName);
            }
            */
        }
    }
}
