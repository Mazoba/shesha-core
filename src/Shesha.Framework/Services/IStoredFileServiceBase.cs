using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Abp.Domain.Entities;
using JetBrains.Annotations;
using Shesha.Domain;

namespace Shesha.Services
{
    public interface IStoredFileServiceBase<T> /*: IService<T> */where T : StoredFile, new()
    {
        /*
        void MarkDownloaded(T file);
        
                /// <summary>
        /// Returns true is the file exists on the disk
        /// </summary>
        bool Exists(T file);
        
        todo: add encryption support (implement file transformations and store metadata like `IsEncrypted`)
        todo: implement support of subversions ? will be used for signed or stamped files
        todo: implement support of metadata ? example of usage: OCR results
        */
        Task MarkDownloadedAsync(StoredFileVersion fileVersion);

        Task<IList<StoredFile>> GetAttachmentsOfCategoryAsync<TId>([NotNull] IEntity<TId> owner, Int64? fileCategory);
        Task<IList<StoredFile>> GetAttachmentsOfCategoryAsync<TId>(TId id, string typeShortAlias, Int64? fileCategory);
        Task<IList<StoredFile>> GetAttachmentsAsync<TId>(IEntity<TId> owner);
        Task<IList<StoredFile>> GetAttachmentsAsync<TId>(TId id, string typeShortAlias);
        Task<bool> HasAttachmentsOfCategoryAsync<TId>(IEntity<TId> owner, Int64? fileCategory);
        Task<bool> HasAttachmentsOfCategoryAsync<TId>(TId id, string typeShortAlias, Int64? fileCategory);
        Task<Stream> GetStreamAsync(StoredFileVersion fileVersion);
        Task<Stream> GetStreamAsync(StoredFile file);
        Task<Stream> GetStreamAsync(string filePath);
        Stream GetStream(StoredFile file);
        Task<StoredFile> CopyToOwnerAsync<TId>(StoredFile file, IEntity<TId> newOwner, bool throwCopyException = true);

        Task CopyAttachmentsToAsync<TSourceId, TDestinationId>(IEntity<TSourceId> source, IEntity<TDestinationId> destination);

        Task<IList<Int64?>> GetAttachmentsCategoriesAsync<TId>(IEntity<TId> owner);
        Task<StoredFileVersion> GetNewOrDefaultVersionAsync([NotNull] StoredFile file);
        Task RenameFileAsync(StoredFile file, string fileName);
        Task<StoredFileVersion> GetLastVersionAsync(StoredFile file);
        Task<List<StoredFileVersion>> GetFileVersionsAsync(StoredFile file);

        Task UpdateVersionContentAsync(StoredFileVersion version, Stream stream);
        Task<T> SaveFile(Stream stream, string fileName, Action<StoredFile> prepareFileAction = null);

        /// <summary>
        /// Update file content and name
        /// </summary>
        /// <param name="file">Stored file</param>
        /// <param name="stream">Stream with new file content</param>
        /// <param name="fileName">New file name</param>
        /// <returns></returns>
        Task<StoredFileVersion> UpdateFile(T file, Stream stream, string fileName);

        /// <summary>
        /// Returns tru if file exists in the DB
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<bool> FileExists(Guid id);

        /// <summary>
        /// Get file by id or null if missing
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        T GetOrNull(Guid id);

        Dictionary<string, StoredFile> MakeUniqueFileNames(IList<StoredFile> files);


        /// <summary>
        /// Delete Stored File
        /// </summary>
        Task DeleteAsync(StoredFile storedFile);

        /// <summary>
        /// Delete Stored File
        /// </summary>
        void Delete(StoredFile storedFile);
    }
}
