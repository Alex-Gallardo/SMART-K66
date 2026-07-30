namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class _202511042043344_SeAgregaCampoBodegaTablaPedidoDetalleK66 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Pedido_Detalle_K66", "WarehouseId", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Pedido_Detalle_K66", "WarehouseId");
        }
    }
}
