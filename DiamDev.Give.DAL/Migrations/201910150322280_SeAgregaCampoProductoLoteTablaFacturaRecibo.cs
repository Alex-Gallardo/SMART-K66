namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaCampoProductoLoteTablaFacturaRecibo : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Factura", "Producto_Lote", c => c.Boolean(nullable: false));
            AddColumn("dbo.Recibo", "Producto_Lote", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Recibo", "Producto_Lote");
            DropColumn("dbo.Factura", "Producto_Lote");
        }
    }
}
