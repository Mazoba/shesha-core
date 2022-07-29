using FluentMigrator;

namespace Shesha.Migrations.EnterpriseMigration
{
    [Migration(20220719122199)]
    public class M20220719122199 : AutoReversingMigration
    {
        public override void Up()
        {
            Rename.Table("Core_BankAccounts").To("entpr_BankAccounts");
            Rename.Table("Core_DistributionLists").To("entpr_DistributionLists");
            Rename.Table("Core_DistributionListItems").To("entpr_DistributionListItems");
            Rename.Table("Core_LogonMessages").To("entpr_LogonMessages");
            Rename.Table("Core_LogonMessageAuditItems").To("entpr_LogonMessageAuditItems");
            Rename.Table("Core_Orders").To("entpr_Orders");
            Rename.Table("Core_OrganisationBankAccounts").To("entpr_OrganisationBankAccounts");
            Rename.Table("Core_OrganisationPosts").To("entpr_OrganisationPosts");
            Rename.Table("Core_OrganisationPostAppointments").To("entpr_OrganisationPostAppointments");
            Rename.Table("Core_OrganisationPostLevels").To("entpr_OrganisationPostLevels");
            Rename.Table("Core_Periods").To("entpr_Periods");
            Rename.Table("Core_Services").To("entpr_Services");

            Rename.Table("Core_ImportResults").To("Frwk_ImportResults");
        }
    }
}
