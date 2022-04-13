using System;
using FluentMigrator;

namespace Shesha.Migrations.Abp
{
    [Migration(20220407184300)]
    public class M20220407184300 : Migration
    {
        public override void Up()
        {
            // increase size because some Entities Audit stored Name as an entity id (eg Setting)
            Alter.Column("EntityId").OnTable("AbpEntityChanges").AsString(512).Nullable();
        }

        public override void Down()
        {
            throw new NotImplementedException();
        }
    }
}
