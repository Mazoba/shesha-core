using FluentMigrator;
using Shesha.FluentMigrator;
using System;

namespace Shesha.Migrations.EnterpriseMigration
{
    [Migration(20220719153899)]
    public class M20220719153899 : Migration
    {
        public override void Down()
        {
            throw new NotImplementedException();
        }

        public override void Up()
        {
            Alter.Table("entpr_Sequences").AddDeletionAuditColumns();
        }
    }
}
