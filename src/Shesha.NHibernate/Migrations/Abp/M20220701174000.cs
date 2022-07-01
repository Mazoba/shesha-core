using FluentMigrator;
using Shesha.FluentMigrator;

namespace Shesha.Migrations.Abp
{
    [Migration(2022070117400)]
    public class M20220701174000 : AutoReversingMigration
    {
        public override void Up()
        {
            Alter.Table("AbpAuditLogs").AlterColumn("ExceptionMessage").AsStringMax().Nullable();
        }
    }
}
