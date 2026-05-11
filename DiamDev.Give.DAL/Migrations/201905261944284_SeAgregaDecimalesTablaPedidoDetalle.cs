namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaDecimalesTablaPedidoDetalle : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Pedido_Detalle", "Precio_Costo", c => c.Decimal(nullable: false, precision: 18, scale: 4));
            AlterColumn("dbo.Pedido_Detalle", "Precio", c => c.Decimal(nullable: false, precision: 18, scale: 4));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Pedido_Detalle", "Precio", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AlterColumn("dbo.Pedido_Detalle", "Precio_Costo", c => c.Decimal(nullable: false, precision: 18, scale: 2));
        }
    }
}
