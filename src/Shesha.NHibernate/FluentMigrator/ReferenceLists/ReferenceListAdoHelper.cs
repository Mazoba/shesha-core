using System;
using System.Data;

namespace Shesha.FluentMigrator.ReferenceLists
{
    /// <summary>
    /// ReferenceList ADO provider
    /// </summary>
    public class ReferenceListAdoHelper
    {
        private readonly IDbConnection _connection;
        private readonly IDbTransaction _transaction;

        public ReferenceListAdoHelper(IDbConnection connection, IDbTransaction transaction)
        {
            _connection = connection;
            _transaction = transaction;
        }

        private void ExecuteNonQuery(string sql, Action<IDbCommand> prepareAction = null) 
        {
            ExecuteCommand(sql, command => {
                prepareAction?.Invoke(command);
                command.ExecuteNonQuery();
            });
        }

        private T ExecuteScalar<T>(string sql, Action<IDbCommand> prepareAction = null)
        {
            T result = default(T);
            ExecuteCommand(sql, command => {
                prepareAction?.Invoke(command);
                result = (T)command.ExecuteScalar();
            });
            return result;
        }

        private void ExecuteCommand(string sql, Action<IDbCommand> action)
        {
            using (var command = _connection.CreateCommand())
            {
                command.Transaction = _transaction;
                command.CommandText = sql;

                action.Invoke(command);
            }
        }

        public void DeleteReferenceListItems(string @namespace, string name)
        {
            var id = GetReferenceListId(@namespace, name);
            if (id == null)
                return;

            ExecuteNonQuery(@"delete from Frwk_ReferenceListItems where ReferenceListId = @ReferenceListId",
                command => {
                    command.AddParameter("@ReferenceListId", id);
                }
            );
        }

        public void DeleteReferenceListItem(string @namespace, string name, Int64 itemValue)
        {
            var id = GetReferenceListId(@namespace, name);
            if (id == null)
                return;

            ExecuteNonQuery(@"delete from Frwk_ReferenceListItems where ReferenceListId = @ReferenceListId and ItemValue = @ItemValue",
                command => {
                    command.AddParameter("@ReferenceListId", id);
                    command.AddParameter("@ItemValue", itemValue);
                }
            );
        }

        public Guid? GetReferenceListId(string @namespace, string name)
        {
            return ExecuteScalar<Guid?>(@"select Id from Frwk_ReferenceLists where Namespace = @Namespace and Name = @Name", command => {
                command.AddParameter("@namespace", @namespace);
                command.AddParameter("@name", name);
            });
        }

        public void DeleteReferenceList(string @namespace, string name)
        {
            ExecuteNonQuery(@"delete from Frwk_ReferenceLists where Namespace = @Namespace and Name = @Name",
                command => {
                    command.AddParameter("@namespace", @namespace);
                    command.AddParameter("@name", name);
                }
            );
        }

        public Guid InsertReferenceListItem(Guid refListId, ReferenceListItemDefinition item)
        {
            var id = Guid.NewGuid();
            var sql = @"INSERT INTO Frwk_ReferenceListItems
           (Id
           ,TenantId
           ,Description
           ,HardLinkToApplication
           ,Item
           ,ItemValue
           ,OrderIndex
           ,ParentId
           ,ReferenceListId)
     VALUES
           (@Id
           ,@TenantId
           ,@Description
           ,@HardLinkToApplication
           ,@Item
           ,@ItemValue
           ,@OrderIndex
           ,@ParentId
           ,@ReferenceListId)";

            ExecuteNonQuery(sql, command => {
                command.AddParameter("@Id", id);
                command.AddParameter("@TenantId", null);
                command.AddParameter("@Description", item.Description);
                command.AddParameter("@HardLinkToApplication", 0);
                command.AddParameter("@Item", item.Item);
                command.AddParameter("@ItemValue", item.ItemValue);
                command.AddParameter("@OrderIndex", item.OrderIndex ?? item.ItemValue);
                command.AddParameter("@ParentId", null);
                command.AddParameter("@ReferenceListId", refListId);
            });

            return id;
        }

        public Guid InsertReferenceList(string @namespace, string name, string description)
        {
            var id = Guid.NewGuid();
            var sql = @"INSERT INTO Frwk_ReferenceLists
           (Id
           ,TenantId
           ,Description
           ,Name
           ,Namespace
           ,HardLinkToApplication)
     VALUES
           (
		   @id
           ,@tenantId
           ,@description
           ,@name
           ,@namespace
           ,@hardLinkToApplication
		   )";

            ExecuteNonQuery(sql, command => {
                command.AddParameter("@id", id);
                command.AddParameter("@tenantId", null);
                command.AddParameter("@description", description);
                command.AddParameter("@name", name);
                command.AddParameter("@namespace", @namespace);
                command.AddParameter("@hardLinkToApplication", 0);

            });
            return id;
        }
    }
}
