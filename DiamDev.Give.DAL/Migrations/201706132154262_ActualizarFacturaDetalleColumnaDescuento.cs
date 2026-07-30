namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ActualizarFacturaDetalleColumnaDescuento : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Factura_Detalle", "Descuento", c => c.Decimal(precision: 18, scale: 2));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Factura_Detalle", "Descuento");
        }
    }
}
