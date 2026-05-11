namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgreganCamposPedidoDetalleK66 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Pedido_Detalle_K66", "Precio_Original", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AddColumn("dbo.Pedido_Detalle_K66", "Precio_Cambiado", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Pedido_Detalle_K66", "Precio_Cambiado");
            DropColumn("dbo.Pedido_Detalle_K66", "Precio_Original");
        }
    }
}
