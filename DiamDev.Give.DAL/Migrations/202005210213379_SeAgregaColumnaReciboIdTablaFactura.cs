namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaColumnaReciboIdTablaFactura : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Factura", "ReciboId", c => c.Long());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Factura", "ReciboId");
        }
    }
}
