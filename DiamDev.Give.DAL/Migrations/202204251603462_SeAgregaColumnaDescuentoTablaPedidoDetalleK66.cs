namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaColumnaDescuentoTablaPedidoDetalleK66 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Pedido_Detalle_K66", "Descuento", c => c.Decimal(nullable: false, precision: 18, scale: 2));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Pedido_Detalle_K66", "Descuento");
        }
    }
}
