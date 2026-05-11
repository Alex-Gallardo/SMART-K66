namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaCampoAdministrativoTablaToken : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Token", "Administrativo", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Token", "Administrativo");
        }
    }
}
