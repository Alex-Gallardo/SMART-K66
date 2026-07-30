namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregoCampoPagadaFactura : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Factura", "Pagada", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Factura", "Pagada");
        }
    }
}
