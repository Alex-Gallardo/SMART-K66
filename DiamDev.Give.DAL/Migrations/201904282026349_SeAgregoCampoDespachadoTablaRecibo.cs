namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoCampoDespachadoTablaRecibo : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Recibo", "Despachado", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Recibo", "Despachado");
        }
    }
}
