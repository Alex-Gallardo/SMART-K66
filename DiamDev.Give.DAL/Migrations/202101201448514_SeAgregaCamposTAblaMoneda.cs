namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaCamposTAblaMoneda : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Moneda", "Codigo", c => c.String());
            AddColumn("dbo.Moneda", "Simbolo", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Moneda", "Simbolo");
            DropColumn("dbo.Moneda", "Codigo");
        }
    }
}
