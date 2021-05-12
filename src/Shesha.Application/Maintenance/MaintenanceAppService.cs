﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Authorization;
using Abp.Dependency;
using Abp.ObjectMapping;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using NHibernate;
using NHibernate.Transform;
using Shesha.Authorization;
using Shesha.Configuration;
using Shesha.Extensions;
using Shesha.NHibernate.Utilites;
using Shesha.Services;
using Shesha.Web.DataTable;

namespace Shesha.Maintenance
{
    /// <summary>
    /// Maintenance service
    /// </summary>
    [AbpAuthorize(ShaPermissionNames.Pages_Maintenance)]
    public class MaintenanceAppService : ApplicationService, IMaintenanceAppService
    {

        private string DbBackupsFolder => StaticContext.IocManager.Resolve<IWebHostEnvironment>().GetAppConfiguration()["DbBackupsFolder"] ?? "c:\\SQL Database Backups";

        private ISession CurrentSession => StaticContext.IocManager.Resolve<ISessionFactory>().GetCurrentSession();

        private Lazy<string> DbName = new Lazy<string>(() => StaticContext.IocManager.Resolve<ISessionFactory>()
            .GetCurrentSession()
            .CreateSQLQuery("select DB_NAME()")
            .UniqueResult<string>());

        /// <summary>
        /// Custom Index table configuration 
        /// </summary>
        public static DataTableConfig BackupFilesIndex()
        {
            var table = new DataTableConfig<BackupFileDto>("BackupFiles_Index", p => p.Id);

            table.AddProperty(e => e.FileName);
            return table;
        }

        /// <summary>
        /// Returns data for the DateTable control
        /// </summary>
        [HttpPost]
        public async Task<DataTableData> GetData(DataTableGetDataInput input, CancellationToken cancellationToken)
        {
            var mapper = StaticContext.IocManager.Resolve<IObjectMapper>();
            var tableConfig = BackupFilesIndex();

            var items = (await GetBackupsList()).Select(x => new { FileName = x }).ToList();

            var totalRowsBeforeFilter = items.Count();

            // Dynamic filter
            if (!string.IsNullOrEmpty(input.QuickSearch))
            {
                var properties = tableConfig.AuthorizedColumns.Where(x => x.IsVisible).Select(x => x.FilterPropertyName)
                    .ToArray();
                if (properties.Length > 0)
                {
                    items = items.LikeDynamic(properties, input.QuickSearch).ToList();
                }
            }

            var totalRows = items.Count();

            var totalPages = (int)Math.Ceiling((double)items.Count() / input.PageSize);

            var takeCount = input.PageSize > -1 ? input.PageSize : int.MaxValue;
            var skipCount = Math.Max(0, (input.CurrentPage - 1) * takeCount);

            var sort = input.Sorting.FirstOrDefault();
            if (sort != null)
            {
                items = items.OrderByDynamic(sort.Id, sort.Desc ? "desc" : "asc").ToList();
            }

            items = items.Skip(skipCount).Take(takeCount).ToList();

            var dataRows = new List<Dictionary<string, object>>();

            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                dataRows.Add(new Dictionary<string, object>() { { "FileName", item.FileName } });
            }

            var result = new DataTableData
            {
                TotalRowsBeforeFilter = totalRowsBeforeFilter,
                TotalRows = totalRows,
                TotalPages = totalPages,
                //Echo = dataTableParam.sEcho,
                Rows = dataRows
            };

            return result;
        }

        [HttpGet]
        public async Task<List<string>> GetBackupsList()
        {
            var query = CurrentSession
                .CreateSQLQuery("EXEC xp_DirTree :path, 1, 1")
                .SetParameter("path", DbBackupsFolder)
                .SetResultTransformer(Transformers.AliasToBean<SqlFileInfo>());

            var backups = query.List<SqlFileInfo>()
                .Where(f => f.file == 1 && f.subdirectory.EndsWith(".bak", StringComparison.InvariantCultureIgnoreCase))
                .Select(f => f.subdirectory)
                .ToList();

            return backups;
        }

        [HttpGet]
        public async Task<BackupDataDto> DeleteBackup(string fileName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                    return new BackupDataDto() { ErrorMessage = "File Name shouldn't be empty" };

                if (!fileName.EndsWith(".bak")) fileName = fileName + ".bak";

                var fullName = Path.Combine(DbBackupsFolder, fileName);

                if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                    return new BackupDataDto() { ErrorMessage = "File Name contains invalid characters" };

                var connStr = NHibernateUtilities.ConnectionString;

                if (string.IsNullOrWhiteSpace(connStr))
                    return new BackupDataDto() { ErrorMessage = "Connection string not found" };

                using (var con = new SqlConnection(connStr))
                {
                    con.Open();

                    var sql = $@"EXEC('xp_cmdshell ''del ""{fullName}""''')";

                    var backupCommand = new SqlCommand(sql, con) { CommandTimeout = 60 };
                    // 1 min to delete file
                    backupCommand.ExecuteNonQuery();
                }

                return new BackupDataDto() { BackupPath = "Backup file deleted" };
            }
            catch (Exception e)
            {
                Logger.Error("", e);
                return new BackupDataDto() { ErrorMessage = e.Message };
            }
        }


        [HttpGet]
        public async Task<BackupDataDto> GetBackupData()
        {
            return new BackupDataDto()
            {
                BackupPath = DbBackupsFolder
            };
        }

        /// <summary>
        /// Backup DB
        /// </summary>
        /// <param name="fileName">Filename of DB backup</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<BackupDataDto> BackupDB(string fileName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                    return new BackupDataDto() { ErrorMessage = "File Name shouldn't be empty" };

                if (!fileName.EndsWith(".bak")) fileName = fileName + ".bak";

                var fullName = Path.Combine(DbBackupsFolder, fileName);

                if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                    return new BackupDataDto() { ErrorMessage = "File Name contains invalid characters" };

                var connStr = NHibernateUtilities.ConnectionString;

                if (string.IsNullOrWhiteSpace(connStr))
                    return new BackupDataDto() { ErrorMessage = "Connection string not found" };

                using (var con = new SqlConnection(connStr))
                {
                    con.Open();

                    var sql =
                        string.Format(@"BACKUP DATABASE {0} TO DISK = '{1}' WITH FORMAT, MEDIANAME = '{0}', NAME = 'Full Backup of {0}'", DbName.Value, fullName);

                    var backupCommand = new SqlCommand(sql, con) { CommandTimeout = 30 * 60 };
                    // 30 mins to backup
                    backupCommand.ExecuteNonQuery();
                }

                return new BackupDataDto() { BackupPath = "Backup has successfully created" };
            }
            catch (Exception e)
            {
                Logger.Error("", e);
                return new BackupDataDto() { ErrorMessage = e.Message };
            }
        }

        [HttpGet]
        public async Task<BackupDataDto> RestoreDb(string fileName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                    return new BackupDataDto() { ErrorMessage = "File Name shouldn't be empty" };

                var connStr = NHibernateUtilities.ConnectionString;

                if (string.IsNullOrWhiteSpace(connStr))
                    return new BackupDataDto() { ErrorMessage = "Connection string not found" };

                fileName = System.IO.Path.Combine(DbBackupsFolder, fileName);

                var sql =
                    $@"EXEC master.dbo.xp_fileexist :backup WITH RESULT SETS(({nameof(SqlFileExistanceInfo.FileExists)} bit, {nameof(SqlFileExistanceInfo.FileIsADirectory)} bit, {nameof(SqlFileExistanceInfo.ParentDirectoryExists)} bit))";
                var query = CurrentSession
                    .CreateSQLQuery(sql)
                    .SetParameter("backup", fileName)
                    .SetResultTransformer(Transformers.AliasToBean<SqlFileExistanceInfo>());

                var fileInfo = query.List<SqlFileExistanceInfo>().FirstOrDefault();
                var exists = fileInfo?.FileExists == true && fileInfo?.FileIsADirectory == false;
                if (!exists)
                    return new BackupDataDto() { ErrorMessage = $"Backup file '{fileName}' not found on the server" };


                Restore(connStr, fileName);

                return new BackupDataDto() { BackupPath = "Database successfully restored" };
            }
            catch (Exception e)
            {
                return new BackupDataDto() { ErrorMessage = e.Message };
            }
        }

        internal void Restore(string connectionString, string backUpPath)
        {
            var dbName = DbName.Value;

            //Commit transaction to close all unsaved data and connections before restore
            StaticContext.IocManager.Resolve<ISessionFactory>().GetCurrentSession().Transaction.Commit();
            using (var con = new SqlConnection(connectionString))
            {
                con.Open();

                new SqlCommand("ALTER DATABASE [" + dbName + "] SET Single_User WITH Rollback Immediate", con)
                    .ExecuteNonQuery();

                var restoreCommand =
                    new SqlCommand(
                        "USE [master] RESTORE DATABASE [" + dbName + "] FROM DISK = N'" + backUpPath +
                        "' WITH REPLACE, FILE = 1,  NOUNLOAD,  STATS = 10", con);
                restoreCommand.CommandTimeout = 30 * 60; // 30 mins to restore
                restoreCommand.ExecuteNonQuery();

                new SqlCommand("ALTER DATABASE [" + dbName + "] SET Multi_User", con).ExecuteNonQuery();

            }
        }
    }

    public class SqlFileInfo
    {
        public string subdirectory { get; set; }
        public int depth { get; set; }
        public int file { get; set; }
    }

    public class SqlFileExistanceInfo
    {
        public bool FileExists { get; set; }
        public bool FileIsADirectory { get; set; }
        public bool ParentDirectoryExists { get; set; }
    }

}