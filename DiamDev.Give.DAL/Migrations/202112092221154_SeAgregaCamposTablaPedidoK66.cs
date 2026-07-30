namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaCamposTablaPedidoK66 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Pedido_K66", "CUSTOMER_ORDER_ROWID", c => c.Int());
            AddColumn("dbo.Pedido_K66", "CUSTOMER_ORDER_ID", c => c.String());
            AddColumn("dbo.Pedido_K66", "Sincronizado", c => c.Boolean(nullable: false));
            AddColumn("dbo.Pedido_K66", "Fecha_Hora_Pedido", c => c.DateTime());
            AddColumn("dbo.Pedido_K66", "Fecha_Hora_Ultimo_Intento", c => c.DateTime());
            AddColumn("dbo.Pedido_K66", "Fecha_Hora_Sincronizacion", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Pedido_K66", "Fecha_Hora_Sincronizacion");
            DropColumn("dbo.Pedido_K66", "Fecha_Hora_Ultimo_Intento");
            DropColumn("dbo.Pedido_K66", "Fecha_Hora_Pedido");
            DropColumn("dbo.Pedido_K66", "Sincronizado");
            DropColumn("dbo.Pedido_K66", "CUSTOMER_ORDER_ID");
            DropColumn("dbo.Pedido_K66", "CUSTOMER_ORDER_ROWID");
        }
    }
}
