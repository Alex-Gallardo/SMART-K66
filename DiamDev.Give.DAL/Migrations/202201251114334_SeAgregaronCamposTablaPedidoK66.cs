namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaronCamposTablaPedidoK66 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Pedido_K66", "Tipo_Pedido_Id", c => c.Guid());
            AddColumn("dbo.Pedido_K66", "Orden_Compra_Cliente", c => c.String());
            AddColumn("dbo.Pedido_K66", "Observaciones_Generales", c => c.String());
            AddColumn("dbo.Pedido_K66", "Fecha_Prometida", c => c.DateTime());
            CreateIndex("dbo.Pedido_K66", "Tipo_Pedido_Id");
            AddForeignKey("dbo.Pedido_K66", "Tipo_Pedido_Id", "dbo.Pedido_Tipo_K66", "Tipo_Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Pedido_K66", "Tipo_Pedido_Id", "dbo.Pedido_Tipo_K66");
            DropIndex("dbo.Pedido_K66", new[] { "Tipo_Pedido_Id" });
            DropColumn("dbo.Pedido_K66", "Fecha_Prometida");
            DropColumn("dbo.Pedido_K66", "Observaciones_Generales");
            DropColumn("dbo.Pedido_K66", "Orden_Compra_Cliente");
            DropColumn("dbo.Pedido_K66", "Tipo_Pedido_Id");
        }
    }
}
