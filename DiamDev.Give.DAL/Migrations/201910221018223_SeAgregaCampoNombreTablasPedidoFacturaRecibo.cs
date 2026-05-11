namespace DiamDev.Give.DAL.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeAgregaCampoNombreTablasPedidoFacturaRecibo : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Factura_Detalle", "Nombre", c => c.String(maxLength: 400));
            AddColumn("dbo.Recibo_Detalle", "Nombre", c => c.String(maxLength: 400));
            AddColumn("dbo.Pedido_Detalle", "Nombre", c => c.String(maxLength: 400));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Pedido_Detalle", "Nombre");
            DropColumn("dbo.Recibo_Detalle", "Nombre");
            DropColumn("dbo.Factura_Detalle", "Nombre");
        }
    }
}
